-- =============================================
-- Author:      Zeel Vaghasiya
-- Create date: 04-September-2025
-- Description: Updates current and highest quiz streak for a user.
-- Usage:       select * from recalc_user_streak(<user_id>);
-- Example:     select * from recalc_user_streak(101); -- Recalculate streaks for user 101
-- =============================================

CREATE OR REPLACE FUNCTION recalc_user_streak(p_user_id INT)
RETURNS TABLE(success BOOLEAN) AS $$
BEGIN
    -- Ensure the user has a record in UserPerformanceDetails
    INSERT INTO "UserPerformanceDetails" (
        user_id, total_xp, old_global_rank, new_global_rank,
        current_level, current_streak, highest_streak, created_date
    )
    VALUES (p_user_id, 0, 0, 0, 1, 0, 0, NOW())
    ON CONFLICT (user_id) DO NOTHING;

    -- Recalculate streaks
    UPDATE "UserPerformanceDetails" upd
    SET current_streak = streaks.cur_streak,
        highest_streak = GREATEST(upd.highest_streak, streaks.highest_streak),
        modified_date = NOW()
    FROM (
        SELECT user_id,
               CASE 
                   WHEN MAX(last_play) >= CURRENT_DATE - 1 
                   THEN MAX(CASE WHEN rn = 1 THEN streak_len END) -- last streak length
                   ELSE 0 
               END AS cur_streak,
               COALESCE(MAX(streak_len), 0) AS highest_streak
        FROM (
            SELECT user_id, 
                   MIN(play_date) AS start_date, 
                   MAX(play_date) AS last_play,
                   COUNT(*) AS streak_len,
                   ROW_NUMBER() OVER (PARTITION BY user_id ORDER BY MAX(play_date) DESC) AS rn
            FROM (
                SELECT user_id, play_date,
                       ROW_NUMBER() OVER (PARTITION BY user_id ORDER BY play_date)
                       - EXTRACT(DAY FROM play_date) AS grp
                FROM (
                    SELECT DISTINCT qa.user_id, qa.created_date::DATE AS play_date
                    FROM "QuizAttempted" qa
                    WHERE qa.user_id = p_user_id
                ) d
            ) x
            GROUP BY user_id, grp
        ) sg
        GROUP BY user_id
    ) streaks
    WHERE upd.user_id = streaks.user_id
      AND upd.user_id = p_user_id;

	RETURN QUERY SELECT TRUE AS success;

EXCEPTION
    WHEN OTHERS THEN
        RETURN QUERY SELECT FALSE AS success;
END;
$$ LANGUAGE plpgsql;