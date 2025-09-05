-- =============================================
-- Author:       <Devisha Gajjar>
-- Create date:  <03-Sep-2025>
-- Description:  <Get recent battles of a user including
--               opponent, category, result, score, XP, and profile pic>
-- Usage:        SELECT * FROM get_user_recent_battles(2, 2, 1);
-- =============================================

CREATE OR REPLACE FUNCTION get_user_recent_battles(
    p_user_id INT,             -- ID of the current user
    p_status_draw INT,         -- Enum value for Draw (BattleStatus.Draw)
    p_status_completed INT     -- Enum value for Completed (BattleStatus.Completed)
)
RETURNS TABLE (
    "Opponent" VARCHAR,
    "ProfilePic" VARCHAR,
    "Category" VARCHAR,
    "Result" VARCHAR,
    "YourScore" INT,
    "OpponentScore" INT,
    "XpGained" INT
)
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        -- Opponent name and profile pic
        CASE WHEN bs.user1_id = p_user_id THEN u2.full_name ELSE u1.full_name END::VARCHAR AS "Opponent",
        CASE WHEN bs.user1_id = p_user_id THEN u2.profile_pic ELSE u1.profile_pic END::VARCHAR AS "ProfilePic",

        -- Quiz category name
        qc.category_name::VARCHAR AS "Category",

        -- Battle result (casted to match return type)
        CASE
            WHEN bs.battle_status = p_status_draw THEN 'Draw'::VARCHAR
            WHEN bs.battle_status = p_status_completed AND br.winner_id = p_user_id THEN 'Won'::VARCHAR
            WHEN bs.battle_status = p_status_completed AND br.winner_id IS NOT NULL THEN 'Lost'::VARCHAR
            ELSE 'Unknown'::VARCHAR
        END AS "Result",

        -- Scores
        CASE WHEN bs.user1_id = p_user_id THEN br.user1_corrected_ans ELSE br.user2_corrected_ans END AS "YourScore",
        CASE WHEN bs.user1_id = p_user_id THEN br.user2_corrected_ans ELSE br.user1_corrected_ans END AS "OpponentScore",

        -- XP gained
        CASE
            WHEN bs.battle_status = p_status_draw THEN br.winner_gained_xp
            WHEN bs.battle_status = p_status_completed AND br.winner_id = p_user_id THEN br.winner_gained_xp
            WHEN bs.battle_status = p_status_completed AND br.winner_id IS NOT NULL THEN br.looser_gained_xp
            ELSE 0
        END AS "XpGained"

    FROM "BattleResult" br
    JOIN "BattleStatus" bs ON bs.id = br.battle_status
    JOIN "BattleList" bl ON bs.battle_id = bl.id
    JOIN "Quiz" q ON bl.quiz_id = q.id
    JOIN "QuizCategory" qc ON q.category_id = qc.id
    JOIN "Users" u1 ON bs.user1_id = u1.id
    JOIN "Users" u2 ON bs.user2_id = u2.id

    WHERE (bs.user1_id = p_user_id OR bs.user2_id = p_user_id)
      AND (bs.battle_status = p_status_draw OR bs.battle_status = p_status_completed)

    ORDER BY bs.modified_date DESC
    LIMIT 10;
END;
$$ LANGUAGE plpgsql;

SELECT * FROM get_user_recent_battles(12, 2, 1);
