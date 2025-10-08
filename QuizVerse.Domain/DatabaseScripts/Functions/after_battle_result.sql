-- =======================================================================================
-- Author:       Brjrajsinh Jadeja
-- Create date:  3-October-2025
-- Description:  Processes the result of a battle and updates user performance details.
--               The function performs the following steps:
--               1. Fetches the battle status and participating users.
--               2. Validates that the battle is either Completed (1) or Draw (2).
--               3. Retrieves the battle result (winner and XP gained).
--               4. Determines the XP to be awarded to each user based on the result.
--               5. Inserts default performance records for users if they do not exist.
--               6. Updates each user's total XP and current level according to XP earned.
--               7. Returns TRUE if the operation succeeds, FALSE if any error occurs.
-- Usage:        SELECT * FROM after_battle_result(p_battle_status_id := 123);
-- Returns:      TABLE(success BOOLEAN) — TRUE if successfully processed, FALSE otherwise
-- =======================================================================================

CREATE OR REPLACE FUNCTION after_battle_result(p_battle_status_id INT)
RETURNS TABLE(success BOOLEAN) AS $$
DECLARE
    v_status INT;
    v_user1 INT;
    v_user2 INT;
    v_winner INT;
    v_xp_winner INT;
    v_xp_looser INT;
    v_new_level INT;
    v_xp_user1 INT;
    v_xp_user2 INT;
BEGIN
    -- 1. Get battle status
    SELECT battle_status, user1_id, user2_id
    INTO v_status, v_user1, v_user2
    FROM "BattleStatus"
    WHERE id = p_battle_status_id;

    -- 2. Check status
    IF v_status != 1 AND v_status != 2 THEN  
        RAISE EXCEPTION 'Battle is not marked as completed or draw';
    END IF;

    -- 3. Get result row
    SELECT winner_id, winner_gained_xp, looser_gained_xp
    INTO v_winner, v_xp_winner, v_xp_looser
    FROM "BattleResult"
    WHERE battle_status = p_battle_status_id;

    -- 4. Decide XP for each user
    IF v_status = 1 THEN  -- Completed with a winner
        v_xp_user1 := CASE WHEN v_user1 = v_winner THEN v_xp_winner ELSE v_xp_looser END;
        v_xp_user2 := CASE WHEN v_user2 = v_winner THEN v_xp_winner ELSE v_xp_looser END;
    ELSE  -- Draw
        v_xp_user1 := v_xp_winner;
        v_xp_user2 := v_xp_looser;
    END IF;

    -- 5. Process User1 XP
    INSERT INTO "UserPerformanceDetails" (
        user_id, total_xp, old_global_rank, new_global_rank,
        current_level, current_streak, highest_streak, created_date
    )
    VALUES (v_user1, 0, 0, 0, 1, 0, 0, NOW())
    ON CONFLICT (user_id) DO NOTHING;

    SELECT level_order INTO v_new_level
    FROM "LevelByExp"
    WHERE v_xp_user1 + COALESCE((SELECT total_xp FROM "UserPerformanceDetails" WHERE user_id = v_user1), 0)
          BETWEEN minimum_exp AND maximum_exp
    LIMIT 1;

    UPDATE "UserPerformanceDetails"
    SET total_xp = total_xp + v_xp_user1,
        current_level = COALESCE(v_new_level, current_level),
        modified_date = NOW()
    WHERE user_id = v_user1;

    -- 6. Process User2 XP
    INSERT INTO "UserPerformanceDetails" (
        user_id, total_xp, old_global_rank, new_global_rank,
        current_level, current_streak, highest_streak, created_date
    )
    VALUES (v_user2, 0, 0, 0, 1, 0, 0, NOW())
    ON CONFLICT (user_id) DO NOTHING;

    SELECT level_order INTO v_new_level
    FROM "LevelByExp"
    WHERE v_xp_user2 + COALESCE((SELECT total_xp FROM "UserPerformanceDetails" WHERE user_id = v_user2), 0)
          BETWEEN minimum_exp AND maximum_exp
    LIMIT 1;

    UPDATE "UserPerformanceDetails"
    SET total_xp = total_xp + v_xp_user2,
        current_level = COALESCE(v_new_level, current_level),
        modified_date = NOW()
    WHERE user_id = v_user2;

    -- Success
    RETURN QUERY 
        SELECT TRUE AS success; 
    EXCEPTION 
        WHEN OTHERS THEN RETURN QUERY SELECT FALSE AS success;
END;

$$ LANGUAGE plpgsql;