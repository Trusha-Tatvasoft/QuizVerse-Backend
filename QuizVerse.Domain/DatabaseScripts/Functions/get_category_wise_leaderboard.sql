-- =============================================
-- Author:       Darsh Aswani
-- Create Date:  04-September-2025
-- Description:  Retrieves category-wise leaderboard data with the following logic:
--                 • Calculates average score from:
--                     - QuizAttempted (corrected_que / total_que)
--                     - BattleResult (user_corrected_ans / Quiz.total_question)
--                 • Includes users who have attempted quizzes or battles in the given category
--                 • Excludes:
--                     - Deleted users (Users.is_deleted = TRUE)
--                     - Inactive users (Users.status <> 1)
--                     - Quizzes/Battles whose categories are deleted (QuizCategory.is_deleted = TRUE)
--                 • Returns top 3 users ranked by average score within the category
--                 • Always includes the currently logged-in user (if not already in top 3)
-- Usage:        SELECT * FROM get_category_wise_leaderboard(7, 2);
-- =============================================

CREATE OR REPLACE FUNCTION get_category_wise_leaderboard(p_user_id INT,p_category_id INT)
RETURNS TABLE (
    rank BIGINT,
    user_id INT,
    user_name VARCHAR,
    full_name VARCHAR,
    profile_pic VARCHAR,
    average_score NUMERIC,
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
            COUNT(qa.id) AS quizzes_played,
            ROUND(AVG((qa.corrected_que::DECIMAL / NULLIF(qa.total_que,0)) * 100), 2) as quiz_avg_score
        FROM "QuizAttempted" qa
        JOIN "Quiz" q ON q.id = qa.quiz_id
        JOIN "QuizCategory" qc ON qc.id = q.category_id
        JOIN "Users" u ON u.id = qa.user_id
        WHERE q.category_id = p_category_id
          AND qc.is_deleted = FALSE
          AND u.is_deleted = FALSE
          AND u.status = 1
        GROUP BY qa.user_id
    ),
    battle_xp AS (
        SELECT
            u.id AS user_id,
            COUNT(br.id) AS battles_played,
            ROUND(AVG(
                CASE 
                    WHEN u.id = bs.user1_id 
                        THEN (br.user1_corrected_ans::DECIMAL / NULLIF(q.total_question,0)) * 100
                    WHEN u.id = bs.user2_id 
                        THEN (br.user2_corrected_ans::DECIMAL / NULLIF(q.total_question,0)) * 100
                END
            ), 2) as battle_avg_score
        FROM "BattleResult" br
        JOIN "BattleStatus" bs ON bs.id = br.battle_status
        JOIN "BattleList" bl ON bl.id = bs.battle_id
        JOIN "Quiz" q ON q.id = bl.quiz_id
        JOIN "QuizCategory" qc ON qc.id = q.category_id
        JOIN "Users" u ON (u.id = bs.user1_id OR u.id = bs.user2_id)
        WHERE q.category_id = p_category_id
          AND qc.is_deleted = FALSE
          AND bs.battle_status <> 3
          AND u.is_deleted = FALSE
          AND u.status = 1
        GROUP BY u.id
    ),
    combined AS (
        SELECT
            u.id AS user_id,
            COALESCE(q.quizzes_played,0) AS total_quizzes_played,
            COALESCE(b.battles_played,0) AS total_battles_played,
            q.quiz_avg_score,
            b.battle_avg_score
        FROM "Users" u
        LEFT JOIN quiz_xp q ON q.user_id = u.id
        LEFT JOIN battle_xp b ON b.user_id = u.id
        WHERE u.is_deleted = FALSE 
          AND u.status = 1
    ),
    ranked AS (
        SELECT
            u.id AS user_id,
            u.user_name,
            u.full_name,
            u.profile_pic,
            c.total_quizzes_played,
            c.total_battles_played,
            CASE 
                WHEN (c.total_quizzes_played + c.total_battles_played) > 0 
                THEN ROUND(
                    (
                        (COALESCE(c.quiz_avg_score,0) * c.total_quizzes_played) +
                        (COALESCE(c.battle_avg_score,0) * c.total_battles_played)
                    ) / NULLIF((c.total_quizzes_played + c.total_battles_played),0)
                , 2)
                ELSE NULL
            END AS average_score,
            RANK() OVER (ORDER BY 
                CASE 
                    WHEN (c.total_quizzes_played + c.total_battles_played) > 0 
                    THEN (
                        (COALESCE(c.quiz_avg_score,0) * c.total_quizzes_played) +
                        (COALESCE(c.battle_avg_score,0) * c.total_battles_played)
                    ) / NULLIF((c.total_quizzes_played + c.total_battles_played),0)
                    ELSE 0
                END DESC
            ) AS user_rank
        FROM combined c
        JOIN "Users" u ON u.id = c.user_id
    ),
    top50 AS (
	    SELECT *
	    FROM ranked r
	    WHERE user_rank <= 50 
	      AND COALESCE(r.average_score, 0) > 0
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
	    COALESCE(r.average_score,0),
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
