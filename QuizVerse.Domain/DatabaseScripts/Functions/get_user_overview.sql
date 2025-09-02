-- ==============================================================================
-- Author:       <Devisha Gajjar>
-- Create date:  <26-August-2025>
-- Description:  <Generates a user overview including recent activities, 
--               global rank, best performing quiz category, and longest streak>
-- Usage:        SELECT * FROM get_user_overview(p_user_id);
-- ==============================================================================

CREATE OR REPLACE FUNCTION get_user_overview(p_user_id INT)
RETURNS TABLE (
    "RecentActivityJson" JSONB,
    "GlobalRank" INT,
    "BestCategory" VARCHAR,
    "LongestStreak" INT
) AS $$
BEGIN
    RETURN QUERY
    WITH recent_activities AS (
        SELECT 
            jsonb_build_object(
                'type', 'quiz',
                'description', 'Completed ' || q.name,
                'xp', qa.xp_earned,
                'timestamp', qa.created_date
            ) AS activity,
            qa.created_date AS created_at
        FROM "QuizAttempted" qa
        JOIN "Quiz" q ON q.id = qa.quiz_id
        WHERE qa.user_id = p_user_id

        UNION ALL

        SELECT 
            jsonb_build_object(
                'type', 'battle',
                'description', 
                    CASE 
                        WHEN br.winner_id = p_user_id THEN 'Won battle vs ' || opp.full_name
                        ELSE 'Lost battle vs ' || opp.full_name 
                    END,
                'xp', 
                    CASE 
                        WHEN br.winner_id = p_user_id THEN br.winner_gained_xp 
                        ELSE br.looser_gained_xp 
                    END
            ) AS activity,
            bs.modified_date AS created_at
        FROM "BattleResult" br
        JOIN "BattleStatus" bs ON bs.id = br.battle_status
        JOIN "Users" opp ON (
            CASE 
                WHEN bs.user1_id = p_user_id THEN bs.user2_id 
                ELSE bs.user1_id 
            END
        ) = opp.id
        WHERE bs.user1_id = p_user_id OR bs.user2_id = p_user_id
        ORDER BY created_at DESC
        LIMIT 3
    ),
    user_perf AS (
        SELECT 
            upd.new_global_rank::INT AS global_rank,
            upd.highest_streak::INT AS longest_streak
        FROM "UserPerformanceDetails" upd
        WHERE upd.user_id = p_user_id
        LIMIT 1
    ),
    best_cat AS (
        SELECT 
            qc.category_name
        FROM "QuizAttempted" qa
        JOIN "Quiz" q ON q.id = qa.quiz_id
        JOIN "QuizCategory" qc ON qc.id = q.category_id
        WHERE qa.user_id = p_user_id
        GROUP BY qc.category_name
        ORDER BY SUM(qa.xp_earned) DESC
        LIMIT 1
    )
    SELECT 
        (SELECT jsonb_agg(activity ORDER BY created_at DESC) FROM recent_activities) AS "RecentActivityJson",
        user_perf.global_rank AS "GlobalRank",
        best_cat.category_name AS "BestCategory",
        user_perf.longest_streak AS "LongestStreak"
    FROM user_perf, best_cat;
END;
$$ LANGUAGE plpgsql;

SELECT * FROM get_user_overview(2)