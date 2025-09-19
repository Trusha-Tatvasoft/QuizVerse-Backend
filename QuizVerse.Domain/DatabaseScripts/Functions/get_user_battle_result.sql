-- =============================================
-- Author:       Bhumi Shah
-- Create Date:  19-September-2025
-- Description:  Fetches battle result details for a given user and battle ID, including:
--                 • Validates that battle exists and is not deleted
--                 • Ensures the battle has finished (not in Running status)
--                 • Fetches the latest battle result (winner, XP, attempts)
--                 • Resolves player vs opponent usernames and profile pictures
--                 • Retrieves quiz title via "BattleList" → "Quiz" mapping
--                 • Determines if current user has won the battle
--                 • Returns attempted question counts and earned XP
--                 • Raises exceptions for missing battle, running status, 
--                   missing result, or missing quiz mapping
-- Usage:        SELECT * FROM get_user_battle_result(12, 7, 3);
-- =============================================

CREATE OR REPLACE FUNCTION get_user_battle_result(
    p_battle_id INT,
    p_login_user_id INT,
    p_status_running INT
)
RETURNS TABLE (
    "BattleName" VARCHAR,
    "OpponentUserName" VARCHAR,
    "PlayerProfile" VARCHAR,
    "OpponentProfile" VARCHAR,
    "BattleStatus" INT,
    "IsWin" BOOLEAN,
    "PlayerAttemptedQuestions" INT,
    "OpponentAttemptedQuestions" INT,
    "PlayerEarnedXP" INT
)
LANGUAGE plpgsql
AS $$
DECLARE
    bs RECORD;
    br RECORD;
    player_user RECORD;
    opponent_user RECORD;
    is_user1 BOOLEAN;
    quiz_title TEXT;
BEGIN
    -- Step 1: Find latest battle status row
    SELECT * INTO bs
    FROM "BattleStatus"
    WHERE battle_id = p_battle_id
      AND (user1_id = p_login_user_id OR user2_id = p_login_user_id)
      AND is_deleted = FALSE
	ORDER BY modified_date DESC NULLS LAST
    LIMIT 1;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Battle not found.'
            USING ERRCODE = 'P0001';
    END IF;

    -- Step 2: Check running status
    IF bs.battle_status = p_status_running THEN
        RAISE EXCEPTION 'Battle is still running, wait for some time for result!'
            USING ERRCODE = 'P0001';
    END IF;

    -- Step 3: Get battle result
    SELECT * INTO br
    FROM "BattleResult"
    WHERE battle_status = bs.id
    LIMIT 1;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Battle result not found.'
            USING ERRCODE = 'P0001';
    END IF;

    -- Step 4: Determine player and opponent
    is_user1 := (bs.user1_id = p_login_user_id);

    IF is_user1 THEN
        SELECT user_name, profile_pic INTO player_user 
        FROM "Users" WHERE id = bs.user1_id;

        SELECT user_name, profile_pic INTO opponent_user 
        FROM "Users" WHERE id = bs.user2_id;
    ELSE
        SELECT user_name, profile_pic INTO player_user 
        FROM "Users" WHERE id = bs.user2_id;

        SELECT user_name, profile_pic INTO opponent_user 
        FROM "Users" WHERE id = bs.user1_id;
    END IF;

    -- Step 5: Get battle name from Quiz
    SELECT q.name INTO quiz_title
    FROM "BattleList" bl
    JOIN "Quiz" q ON q.id = bl.quiz_id
    WHERE bl.id = bs.battle_id;

    IF quiz_title IS NULL THEN
        RAISE EXCEPTION 'Quiz title not found for this battle.'
            USING ERRCODE = 'P0001';
    END IF;

    -- Step 6: Return data
    "BattleName" := quiz_title;
    "OpponentUserName" := opponent_user.user_name;
    "PlayerProfile" := player_user.profile_pic;
    "OpponentProfile" := opponent_user.profile_pic;
    "BattleStatus" := bs.battle_status;
    "IsWin" := (br.winner_id IS NOT NULL AND br.winner_id = p_login_user_id);
    "PlayerAttemptedQuestions" := CASE WHEN is_user1 THEN br.user1_corrected_ans ELSE br.user2_corrected_ans END;
    "OpponentAttemptedQuestions" := CASE WHEN is_user1 THEN br.user2_corrected_ans ELSE br.user1_corrected_ans END;
    "PlayerEarnedXP" := CASE 
                          WHEN br.winner_id = p_login_user_id THEN br.winner_gained_xp
                          ELSE br.looser_gained_xp
                        END;

    RETURN NEXT;
END;
$$;
