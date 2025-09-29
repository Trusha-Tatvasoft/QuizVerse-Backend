-- =============================================
-- Author:      <Zeel Vaghasiya>
-- Create date: <21-Auguest-2025>
-- Description: <Get aggregated user dashboard metrics including user name,
--               quizzes completed, total XP, win rate, and current rank>
-- Usage:       SELECT * FROM get_user_dashboard_metrics(p_user_id);
-- =============================================

CREATE OR REPLACE FUNCTION get_user_dashboard_metrics(p_user_id INT)
RETURNS TABLE (
    user_name VARCHAR,
    quizzes_completed INT,
    total_xp INT,
    win_rate NUMERIC,
    current_rank INT
)
AS $$
BEGIN
    -- User Name
    user_name := (
        SELECT u.user_name
        FROM "Users" u
        WHERE u.id = p_user_id
    );

    -- Quizzes Completed
    quizzes_completed := (
        SELECT COUNT(*)
        FROM "QuizAttempted" qa
        WHERE qa.user_id = p_user_id
    );

    -- Total XP
    total_xp := COALESCE((
        SELECT upd.total_xp
        FROM "UserPerformanceDetails" upd
        WHERE upd.user_id = p_user_id
        ORDER BY upd.created_date DESC
        LIMIT 1
    ), 0);

    -- Win Rate
    win_rate := COALESCE((
        SELECT ROUND(
            (COUNT(*) FILTER (WHERE br.winner_id = p_user_id) * 100.0) / NULLIF(COUNT(*), 0),
            2
        )
        FROM "BattleResult" br
        JOIN "BattleStatus" bs ON bs.id = br.battle_status
        WHERE (bs.user1_id = p_user_id OR bs.user2_id = p_user_id)
          AND bs.battle_status IN (1, 2) -- 1 = completed, 2 = draw
    ), 0);

    -- Current Rank
    current_rank := COALESCE((
        SELECT upd.new_global_rank
        FROM "UserPerformanceDetails" upd
        WHERE upd.user_id = p_user_id
        ORDER BY upd.created_date DESC
        LIMIT 1
    ), 0);

    RETURN NEXT;
END;
$$ LANGUAGE plpgsql;