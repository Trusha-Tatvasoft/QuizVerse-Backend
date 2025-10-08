-- =============================================
-- Author:      Zeel Vaghasiya
-- Create date: 05-September-2025
-- Description: Checks user activity and awards badges based on quizzes, battles, categories, difficulties, first attempts, and perfect scores.
-- Usage:       select * from check_and_award_badges(<user_id>);
-- Example:     select * from check_and_award_badges(101); -- Awards badges for user 101
-- =============================================

CREATE OR REPLACE FUNCTION public.check_and_award_badges(
    p_user_id integer
)
RETURNS TABLE(success BOOLEAN)
LANGUAGE plpgsql
AS $$
DECLARE
    v_quiz_count        INT := 0;
    v_battle_count      INT := 0;
    v_category_counts   JSONB := '{}'::JSONB;
    v_difficulty_counts JSONB := '{}'::JSONB;
    v_first_quiz        BOOLEAN := FALSE;
    v_first_battle      BOOLEAN := FALSE;
    v_perfect_quiz      INT := 0;
    v_total_xp_to_add   INT := 0;
    v_total_xp          INT := 0;
    v_new_level         INT;
    v_new_badges_count  INT := 0;
BEGIN
    -- 1. Total quizzes attempted
    SELECT COUNT(*) INTO v_quiz_count
    FROM "QuizAttempted"
    WHERE user_id = p_user_id;

    -- 2. Total battles attempted
    SELECT COUNT(*) INTO v_battle_count
    FROM "BattleStatus"
    WHERE user1_id = p_user_id OR user2_id = p_user_id;

    -- 3. Category-wise counts
    SELECT COALESCE(jsonb_object_agg(category_id, cnt), '{}'::JSONB)
    INTO v_category_counts
    FROM (
        SELECT q.category_id, COUNT(*) AS cnt
        FROM "QuizAttempted" qa
        JOIN "Quiz" q ON q.id = qa.quiz_id
        WHERE qa.user_id = p_user_id
        GROUP BY q.category_id
        UNION ALL
        SELECT q.category_id, COUNT(*)
        FROM "BattleStatus" bs
        JOIN "BattleList" bl ON bl.id = bs.battle_id
        JOIN "Quiz" q ON q.id = bl.quiz_id
        WHERE bs.user1_id = p_user_id OR bs.user2_id = p_user_id
        GROUP BY q.category_id
    ) sub;

    -- 4. Difficulty-wise counts
    SELECT COALESCE(jsonb_object_agg(difficulty_level_id, cnt), '{}'::JSONB)
    INTO v_difficulty_counts
    FROM (
        SELECT q.difficulty_level_id, COUNT(*) AS cnt
        FROM "QuizAttempted" qa
        JOIN "Quiz" q ON q.id = qa.quiz_id
        WHERE qa.user_id = p_user_id
        GROUP BY q.difficulty_level_id
        UNION ALL
        SELECT q.difficulty_level_id, COUNT(*)
        FROM "BattleStatus" bs
        JOIN "BattleList" bl ON bl.id = bs.battle_id
        JOIN "Quiz" q ON q.id = bl.quiz_id
        WHERE bs.user1_id = p_user_id OR bs.user2_id = p_user_id
        GROUP BY q.difficulty_level_id
    ) sub;

    -- 5. First quiz and first battle flags
    SELECT EXISTS (
        SELECT 1 FROM "QuizAttempted" qa WHERE qa.user_id = p_user_id
    ) INTO v_first_quiz;

    SELECT EXISTS (
        SELECT 1 FROM "BattleStatus" bs 
        WHERE bs.user1_id = p_user_id OR bs.user2_id = p_user_id
    ) INTO v_first_battle;

    -- 6. Perfect quizzes
    SELECT COUNT(*) INTO v_perfect_quiz
    FROM "QuizAttempted" qa
    WHERE qa.user_id = p_user_id
      AND qa.corrected_que = qa.total_que;

    -- 7. Award eligible badges
    CREATE TEMP TABLE tmp_new_badges(badge_id INT) ON COMMIT DROP;

    INSERT INTO tmp_new_badges(badge_id)
    SELECT b.id
    FROM "Badges" b
    JOIN "BadgeConditionsMapping" bcm ON bcm.badge_id = b.id
    WHERE b.is_deleted = FALSE
      AND NOT EXISTS (
          SELECT 1 FROM "UserBadgesEarned" ube
          WHERE ube.user_id = p_user_id AND ube.badge_id = b.id
      )
      AND (
          (bcm.condition_type = 1 AND v_battle_count >= bcm.condition_value::INT)
          OR (bcm.condition_type = 2 AND
              v_category_counts ? split_part(bcm.condition_value, ':', 1)
              AND (v_category_counts ->> split_part(bcm.condition_value, ':', 1))::INT >= split_part(bcm.condition_value, ':', 2)::INT
          )
          OR (bcm.condition_type = 3 AND
              v_difficulty_counts ? split_part(bcm.condition_value, ':', 1)
              AND (v_difficulty_counts ->> split_part(bcm.condition_value, ':', 1))::INT >= split_part(bcm.condition_value, ':', 2)::INT
          )
          OR (bcm.condition_type = 4 AND v_quiz_count >= bcm.condition_value::INT)
          OR (bcm.condition_type = 5 AND v_first_quiz)
          OR (bcm.condition_type = 6 AND v_first_battle)
          OR (bcm.condition_type = 7 AND v_perfect_quiz >= bcm.condition_value::INT)
      );
 
    GET DIAGNOSTICS v_new_badges_count = ROW_COUNT;
 
    IF v_new_badges_count > 0 THEN
        INSERT INTO "UserBadgesEarned" (user_id, badge_id, date_earned)
        SELECT p_user_id, badge_id, NOW() FROM tmp_new_badges;
 
        SELECT COALESCE(SUM(b."xp"), 0)
        INTO v_total_xp_to_add
        FROM "Badges" b
        WHERE b."id" IN (SELECT badge_id FROM tmp_new_badges);
 
        IF v_total_xp_to_add > 0 THEN
            UPDATE "UserPerformanceDetails"
            SET "total_xp" = "total_xp" + v_total_xp_to_add,
                "modified_date" = NOW()
            WHERE "user_id" = p_user_id;
 
            SELECT "total_xp" INTO v_total_xp
            FROM "UserPerformanceDetails"
            WHERE "user_id" = p_user_id;
 
            SELECT "level_order" INTO v_new_level
            FROM "LevelByExp"
            WHERE v_total_xp BETWEEN "minimum_exp" AND "maximum_exp"
            LIMIT 1;
 
            IF v_new_level IS NOT NULL THEN
                UPDATE "UserPerformanceDetails"
                SET "current_level" = v_new_level
                WHERE "user_id" = p_user_id;
            END IF;
 
            PERFORM recalc_global_ranks();
        END IF;
    END IF;
 
    RETURN QUERY SELECT TRUE AS success;
EXCEPTION
    WHEN OTHERS THEN
        RETURN QUERY SELECT FALSE AS success;
END;
$$ LANGUAGE plpgsql;