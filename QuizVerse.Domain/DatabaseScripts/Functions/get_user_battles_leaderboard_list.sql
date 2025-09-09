-- =============================================
-- Author:       Bhumi Shah
-- Create Date:  03-September-2025
-- Description:  Returns a leaderboard of users based on their performance in completed battles, including:
--                 • UserName: The username of the player
--                 • TotalWins: The total number of battles won by the user
--                 • WinPercentage: The percentage of battles won (rounded to 2 decimal places),
--                 • TotalXp: The total experience points (XP) earned from battles
--                 • Rank: The user's rank based on total XP, using DENSE_RANK to avoid gaps in ranking
--                 • IsLoggedInUser: Boolean flag indicating if the record belongs to the logged-in user
-- Usage:        SELECT * 
--               FROM get_user_battles_leaderboard_list(
--                    p_completed_battle_status := 1,
--                    p_draw_battle_status := 2,
--                    p_active_user_status := 1,
--                    p_logged_in_user_id := 51
--               );
-- =============================================

CREATE OR REPLACE FUNCTION get_user_battles_leaderboard_list(
    p_completed_battle_status INT,
    p_draw_battle_status INT,
    p_active_user_status INT,
    p_logged_in_user_id INT
)
RETURNS TABLE (
    "UserId" INT,
    "UserName" VARCHAR,
    "TotalWins" INT,
    "WinPercentage" NUMERIC,
    "TotalXp" INT,
    "Rank" INT,
    "IsLoggedInUser" BOOLEAN
)
AS $$
BEGIN
    RETURN QUERY
    WITH battle_data AS (
        SELECT
            u.id AS user_id,
            u.user_name AS username,
            br.winner_id,
            CASE
                WHEN br.winner_id = u.id THEN br.winner_gained_xp
                WHEN br.winner_id IS NULL THEN br.winner_gained_xp
                ELSE br.looser_gained_xp
            END AS gained_xp,
            CASE WHEN br.winner_id = u.id THEN 1 ELSE 0 END AS is_win,
            CASE WHEN br.winner_id IS NULL THEN 1 ELSE 0 END AS is_draw
        FROM "BattleStatus" bs
        JOIN "BattleList" bl ON bl.id = bs.battle_id AND bl.is_deleted = false
        JOIN "Quiz" q ON q.id = bl.quiz_id AND q.is_deleted = false
        JOIN "QuizCategory" qc ON qc.id = q.category_id AND qc.is_deleted = false
        JOIN "BattleResult" br ON br.battle_status = bs.id
        JOIN "Users" u ON u.id = bs.user1_id AND u.is_deleted = false AND u.status = p_active_user_status
        WHERE bs.is_deleted = false
          AND (bs.battle_status = p_completed_battle_status OR bs.battle_status = p_draw_battle_status)
          AND DATE_TRUNC('month', bs.modified_date) = DATE_TRUNC('month', CURRENT_TIMESTAMP)
        
        UNION ALL

        SELECT
            u.id AS user_id,
            u.user_name AS username,
            br.winner_id,
            CASE
                WHEN br.winner_id = u.id THEN br.winner_gained_xp
                WHEN br.winner_id IS NULL THEN br.winner_gained_xp
                ELSE br.looser_gained_xp
            END AS gained_xp,
            CASE WHEN br.winner_id = u.id THEN 1 ELSE 0 END AS is_win,
            CASE WHEN br.winner_id IS NULL THEN 1 ELSE 0 END AS is_draw
        FROM "BattleStatus" bs
        JOIN "BattleList" bl ON bl.id = bs.battle_id AND bl.is_deleted = false
        JOIN "Quiz" q ON q.id = bl.quiz_id AND q.is_deleted = false
        JOIN "QuizCategory" qc ON qc.id = q.category_id AND qc.is_deleted = false
        JOIN "BattleResult" br ON br.battle_status = bs.id
        JOIN "Users" u ON u.id = bs.user2_id AND u.is_deleted = false AND u.status = p_active_user_status
        WHERE bs.is_deleted = false
          AND (bs.battle_status = p_completed_battle_status OR bs.battle_status = p_draw_battle_status)
          AND DATE_TRUNC('month', bs.modified_date) = DATE_TRUNC('month', CURRENT_TIMESTAMP)
    ),
    aggregated AS (
        SELECT
            bd.user_id,
            bd.username,
            COUNT(*) FILTER (WHERE bd.is_win = 1) AS total_wins,
            COUNT(*) FILTER (WHERE bd.is_draw = 1) AS total_draws,
            CASE 
                WHEN COUNT(*) = 0 THEN 0
                ELSE (
                    COUNT(*) FILTER (WHERE bd.is_win = 1)
                    / COUNT(*)::NUMERIC
                ) * 100
            END AS win_percentage,
            SUM(bd.gained_xp) AS total_xp
        FROM battle_data bd
        GROUP BY bd.user_id, bd.username
    ),
    ranked AS (
        SELECT
            a.*,
            DENSE_RANK() OVER (ORDER BY a.total_xp DESC) AS rank
        FROM aggregated a
    ),
    top_list AS (
        SELECT
            user_id AS "UserId",
            username AS "UserName",
            total_wins::INT AS "TotalWins",
            ROUND(COALESCE(win_percentage, 0), 2) AS "WinPercentage",
            COALESCE(total_xp, 0)::INT AS "TotalXp",
            rank::INT AS "Rank",
            (user_id = p_logged_in_user_id) AS "IsLoggedInUser"
        FROM ranked
        WHERE rank <= 5
    ),
    logged_in_user AS (
	    SELECT
	        r.user_id AS "UserId",
	        r.username AS "UserName",
	        r.total_wins::INT AS "TotalWins",
	        ROUND(COALESCE(r.win_percentage, 0), 2) AS "WinPercentage",
	        COALESCE(r.total_xp, 0)::INT AS "TotalXp",
	        r.rank::INT AS "Rank",
	        TRUE AS "IsLoggedInUser"
	    FROM ranked r
	    WHERE r.user_id = p_logged_in_user_id
	      AND NOT EXISTS (
	          SELECT 1 FROM top_list t WHERE t."UserId" = r.user_id
	      )
	
	    UNION ALL
	
	    SELECT
	        u.id AS "UserId",
	        u.user_name AS "UserName",
	        0 AS "TotalWins",
	        0 AS "WinPercentage",
	        0::INT AS "TotalXp",
	        0::INT AS "Rank",
	        TRUE AS "IsLoggedInUser"
	    FROM "Users" u
	    WHERE u.id = p_logged_in_user_id
	      AND NOT EXISTS (SELECT 1 FROM ranked WHERE user_id = p_logged_in_user_id)
	)
    SELECT * 
    FROM (
        SELECT * FROM top_list
        UNION ALL
        SELECT * FROM logged_in_user
    ) final_result
    ORDER BY CASE WHEN final_result."Rank" = 0 THEN 999999 ELSE final_result."Rank" END;
END;
$$ LANGUAGE plpgsql;