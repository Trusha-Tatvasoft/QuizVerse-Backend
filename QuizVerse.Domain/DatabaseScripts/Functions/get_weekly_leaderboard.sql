-- =============================================
-- Author:       Darsh Aswani
-- Create Date:  04-September-2025
-- Description:  Retrieves this week's leaderboard data with the following logic:
--                 • Calculates total XP from:
--                     - QuizAttempted.xp_earned
--                     - BattleResult (winner_gained_xp / looser_gained_xp)
--                     - UserBadgesEarned (Badges.xp_value)
--                 • Excludes:
--                     - Deleted users (Users.is_deleted = TRUE)
--                     - Quizzes/Battles whose categories are deleted (QuizCategory.is_deleted = TRUE)
--                 • Returns top 3 users ranked by total XP earned this week
--                 • Always includes the currently logged-in user (if not already in top 3)
-- Usage:        SELECT * FROM get_weekly_leaderboard(10);
-- =============================================

CREATE OR REPLACE FUNCTION get_weekly_leaderboard(p_user_id INT)
RETURNS TABLE (
    rank BIGINT,
    user_id INT,
    user_name VARCHAR,
    full_name VARCHAR,
    profile_pic VARCHAR,
    total_xp BIGINT,
    total_quizzes_played BIGINT,
    total_battles_played BIGINT,
    is_logged_in_user BOOLEAN
)
AS $$
BEGIN
    RETURN QUERY
    WITH quiz_xp AS (
        SELECT
            qa.user_id,
            SUM(qa.xp_earned) AS xp,
            COUNT(qa.id) AS quizzes_played
        FROM "QuizAttempted" qa
        JOIN "Quiz" q ON q.id = qa.quiz_id
        JOIN "QuizCategory" qc ON qc.id = q.category_id
        JOIN "Users" u ON u.id = qa.user_id
        WHERE qa.created_date >= date_trunc('week', CURRENT_DATE)
          AND qc.is_deleted = FALSE
          AND u.is_deleted = FALSE
	      AND u.status = 1          
        GROUP BY qa.user_id
    ),
    battle_xp AS (
        SELECT
            u.id AS user_id,
            SUM(
                CASE 
                    WHEN br.winner_id = u.id THEN br.winner_gained_xp
                    ELSE br.looser_gained_xp
                END
            ) AS xp,
            COUNT(br.id) AS battles_played
        FROM "BattleResult" br
        JOIN "BattleStatus" bs ON bs.id = br.battle_status
        JOIN "BattleList" bl ON bl.id = bs.battle_id
        JOIN "Quiz" q ON q.id = bl.quiz_id
        JOIN "QuizCategory" qc ON qc.id = q.category_id
        JOIN "Users" u ON (u.id = bs.user1_id OR u.id = bs.user2_id)
        WHERE bs.modified_date >= date_trunc('week', CURRENT_DATE)
          AND qc.is_deleted = FALSE
          AND bs.battle_status <> 3
          AND u.is_deleted = FALSE
	      AND u.status = 1          
        GROUP BY u.id
    ),
    badge_xp AS (
        SELECT
            ube.user_id,
            SUM(b.xp) AS xp
        FROM "UserBadgesEarned" ube
        JOIN "Badges" b ON ube.badge_id = b.id
        JOIN "Users" u ON u.id = ube.user_id
        WHERE ube.date_earned >= date_trunc('week', CURRENT_DATE)
          AND u.is_deleted = FALSE
	      AND u.status = 1          
        GROUP BY ube.user_id
    ),
    combined AS (
        SELECT
            u.id AS user_id,
            COALESCE(q.xp,0) + COALESCE(b.xp,0) + COALESCE(bg.xp,0) AS total_xp,
            COALESCE(q.quizzes_played,0) AS total_quizzes_played,
            COALESCE(b.battles_played,0) AS total_battles_played
        FROM "Users" u
        LEFT JOIN quiz_xp q ON q.user_id = u.id
        LEFT JOIN battle_xp b ON b.user_id = u.id
        LEFT JOIN badge_xp bg ON bg.user_id = u.id
        WHERE u.is_deleted = FALSE 
          AND u.status = 1
          AND (
              (COALESCE(q.xp,0) + COALESCE(b.xp,0) + COALESCE(bg.xp,0)) > 0 
              OR u.id = p_user_id
          )
    ),
    ranked AS (
	    SELECT
	        u.id AS user_id,
	        u.user_name,
	        u.full_name,
	        u.profile_pic,
	        c.total_xp,
	        c.total_quizzes_played,
	        c.total_battles_played,
	        DENSE_RANK() OVER (ORDER BY c.total_xp DESC) AS user_rank
	    FROM combined c
	    JOIN "Users" u ON u.id = c.user_id
	),
	top50 AS (
	    SELECT *
	    FROM ranked r
	    WHERE user_rank <= 50 
	      AND r.total_xp >= 0  
		  AND (r.total_quizzes_played > 0 OR r.total_battles_played > 0)
	      AND r.user_id <> p_user_id
	    ORDER BY r.user_rank
	),
	logged_in_user AS (
	    SELECT * FROM ranked r WHERE r.user_id = p_user_id
	)
	SELECT
	    r.user_rank AS rank,
	    r.user_id,
	    r.user_name,
	    r.full_name,
	    r.profile_pic,
	    r.total_xp,
	    r.total_quizzes_played,
	    r.total_battles_played,
	    (r.user_id = p_user_id) AS is_logged_in_user
	FROM (
	    SELECT * FROM top50
	    UNION
	    SELECT * FROM logged_in_user
	) r
	ORDER BY r.user_rank;
END;
$$ LANGUAGE plpgsql;
