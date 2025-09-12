-- =============================================
-- Author:      Zeel Vaghasiya
-- Create date: 05-September-2025
-- Description: Checks user activity and awards badges based on quizzes, battles, categories, difficulties, first attempts, and perfect scores.
-- Usage:       select * from check_and_award_badges(<user_id>);
-- Example:     select * from check_and_award_badges(101); -- Awards badges for user 101
-- =============================================

CREATE OR REPLACE FUNCTION check_and_award_badges(p_user_id INT)
RETURNS TABLE(success BOOLEAN) AS $$
DECLARE
    v_quiz_count        INT := 0;
    v_battle_count      INT := 0;
    v_category_counts   JSONB := '{}'::JSONB;
    v_difficulty_counts JSONB := '{}'::JSONB;
    v_first_quiz        BOOLEAN := FALSE;
    v_first_battle      BOOLEAN := FALSE;
    v_perfect_quiz      INT := 0;
BEGIN
    -- 1. Total quizzes attempted
    SELECT COUNT(*) INTO v_quiz_count
    FROM "QuizAttempted"
    WHERE user_id = p_user_id;

    -- 2. Total battles attempted
    SELECT COUNT(*) INTO v_battle_count
    FROM "BattleStatus"
    WHERE user1_id = p_user_id OR user2_id = p_user_id;

    -- 3. Category-wise counts (quizzes + battles)
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

    -- 4. Difficulty-wise counts (quizzes + battles)
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

    -- 5. First quiz completed?
    SELECT EXISTS (
        SELECT 1 FROM "QuizAttempted" qa WHERE qa.user_id = p_user_id
    ) INTO v_first_quiz;

    -- 6. First battle completed?
    SELECT EXISTS (
        SELECT 1 FROM "BattleStatus" bs 
        WHERE bs.user1_id = p_user_id OR bs.user2_id = p_user_id
    ) INTO v_first_battle;

    -- 7. Perfect score quizzes (all questions correct)
    SELECT COUNT(*) INTO v_perfect_quiz
    FROM "QuizAttempted" qa
    WHERE qa.user_id = p_user_id
      AND qa.corrected_que = qa.total_que;

    -- 8. Award eligible badges
    INSERT INTO "UserBadgesEarned" (user_id, badge_id, date_earned)
    SELECT p_user_id, b.id, NOW()
    FROM "Badges" b
    JOIN "BadgeConditionsMapping" bcm ON bcm.badge_id = b.id
    WHERE b.is_deleted = FALSE
      AND NOT EXISTS (
          SELECT 1 FROM "UserBadgesEarned" ube
          WHERE ube.user_id = p_user_id AND ube.badge_id = b.id
      )
      AND (
          -- Battle count
          (bcm.condition_type = 1 AND v_battle_count >= bcm.condition_value::INT)

          -- Category badges ("categoryId:count")
          OR (bcm.condition_type = 2 AND
              v_category_counts ? split_part(bcm.condition_value, ':', 1)
              AND (v_category_counts ->> split_part(bcm.condition_value, ':', 1))::INT >= split_part(bcm.condition_value, ':', 2)::INT
          )

          -- Difficulty badges ("difficultyId:count")
          OR (bcm.condition_type = 3 AND
              v_difficulty_counts ? split_part(bcm.condition_value, ':', 1)
              AND (v_difficulty_counts ->> split_part(bcm.condition_value, ':', 1))::INT >= split_part(bcm.condition_value, ':', 2)::INT
          )

          -- Quiz count
          OR (bcm.condition_type = 4 AND v_quiz_count >= bcm.condition_value::INT)

          -- First quiz
          OR (bcm.condition_type = 5 AND v_first_quiz)

          -- First battle
          OR (bcm.condition_type = 6 AND v_first_battle)

          -- Perfect score
          OR (bcm.condition_type = 7 AND v_perfect_quiz >= bcm.condition_value::INT)
      );

	 RETURN QUERY SELECT TRUE AS success;
	 
EXCEPTION
    WHEN OTHERS THEN
        RETURN QUERY SELECT FALSE AS success;
END;
$$ LANGUAGE plpgsql;