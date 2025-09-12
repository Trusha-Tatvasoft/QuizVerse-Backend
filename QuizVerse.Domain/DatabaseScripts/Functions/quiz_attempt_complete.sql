-- =============================================
-- Author:      Zeel Vaghasiya
-- Create date: 11-September-2025
-- Description: Inserts a record into QuizAttempted table for a completed quiz.
--              Computes total questions, correct answers, XP earned, percentage, 
--              and grade based on the completed quiz play status.
-- Usage:       select * from quiz_attempt_complete(p_quiz_id, p_user_id, p_time_taken);
-- Example:     select * from quiz_attempt_complete(101, 202, 3600); 
-- =============================================

CREATE OR REPLACE FUNCTION quiz_attempt_complete(
    p_quiz_id INT,
    p_user_id INT,
    p_time_taken INT
)
RETURNS TABLE(success BOOLEAN) AS $$
DECLARE
    v_total_que INT := 0;
    v_corrected_que INT := 0;
    v_xp_earned INT := 0;
    v_percentage NUMERIC(5,2) := 0;
    v_grade_id INT := NULL;
    v_is_completed BOOLEAN := false;
	v_new_level INT := 1;
BEGIN
    -- 1. Check if quiz play status is completed
    SELECT is_completed INTO v_is_completed
    FROM "QuizPlayStatus"
    WHERE quiz_id = p_quiz_id
      AND user_id = p_user_id
    LIMIT 1;

    IF v_is_completed IS NOT TRUE THEN
		RAISE EXCEPTION 'Quiz submission failed: the quiz is not marked as completed for user % and quiz %.', p_user_id, p_quiz_id;
        RETURN QUERY SELECT FALSE AS success;
        RETURN;
    END IF;

    -- 2. Total questions in quiz (ignore deleted)
    SELECT COUNT(*) INTO v_total_que
    FROM "QuizToBaseQuestionMap"
    WHERE quiz_id = p_quiz_id
      AND is_deleted = false;

    -- 3. Correct answers + XP earned (difficulty based)
    SELECT 
        COUNT(*) AS correct_count,
        COALESCE(SUM(qd.xp_gained), 0) AS total_xp
    INTO v_corrected_que, v_xp_earned
    FROM "AttemptedQuizQuestionsAnswer" a
    JOIN "QuizToBaseQuestionMap" qmap ON a.quiz_que_id = qmap.id
    JOIN "BaseQuestions" bq ON qmap.que_id = bq.id
    JOIN "QuestionDifficulty" qd ON bq.que_difficulty_id = qd.id
    WHERE qmap.quiz_id = p_quiz_id
      AND a.is_correct = true
      AND qmap.is_deleted = false
      AND bq.is_deleted = false
      AND qd.is_deleted = false;

    -- 4. Percentage and grade lookup
    IF v_total_que > 0 THEN
        v_percentage := ROUND((v_corrected_que::NUMERIC * 100.0) / v_total_que, 2);

        SELECT id INTO v_grade_id
        FROM "GradeForQuizResult"
        WHERE v_percentage BETWEEN min_percentage AND max_percentage
          AND is_deleted = false
        LIMIT 1;
    END IF;

    -- 5. Insert into QuizAttempted
    INSERT INTO "QuizAttempted" (
        quiz_id, user_id, total_que, corrected_que,
        time_spent, xp_earned, grade, created_date
    )
    VALUES (
        p_quiz_id, p_user_id, v_total_que, v_corrected_que,
        make_interval(secs => COALESCE(p_time_taken, 0)),
        v_xp_earned, v_grade_id, NOW()
    );

	INSERT INTO "UserPerformanceDetails" (
        user_id, total_xp, old_global_rank, new_global_rank,
        current_level, current_streak, highest_streak, created_date
    )
    VALUES (p_user_id, 0, 0, 0, 1, 0, 0, NOW())
    ON CONFLICT (user_id) DO NOTHING;

    SELECT level_order INTO v_new_level
    FROM "LevelByExp"
    WHERE v_xp_earned + COALESCE((SELECT total_xp FROM "UserPerformanceDetails" WHERE user_id = p_user_id), 0)
          BETWEEN minimum_exp AND maximum_exp
    LIMIT 1;

    UPDATE "UserPerformanceDetails"
    SET total_xp = total_xp + v_xp_earned,
        current_level = COALESCE(v_new_level, current_level),
        modified_date = NOW()
    WHERE user_id = p_user_id;
	
	RETURN QUERY SELECT TRUE AS success;
	
EXCEPTION
    WHEN OTHERS THEN
        RETURN QUERY SELECT FALSE AS success;
END;
$$ LANGUAGE plpgsql;