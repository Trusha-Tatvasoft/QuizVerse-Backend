-- =============================================
-- Author:       Bhumi Shah
-- Create Date:  05-Nov-2025
-- Description:  Returns list of active quizzes and battles containing a specific question
-- Usage: SELECT * FROM get_affected_quiz_and_battle_list(103);
-- =============================================

CREATE OR REPLACE FUNCTION get_affected_quiz_and_battle_list(
    p_question_id INT
)
RETURNS TABLE (
    "Id" INT,
    "QuizTitle" VARCHAR,
    "CategoryName" VARCHAR,
    "QuizDifficultyLevel" VARCHAR,
    "TotalQuestion" INT,
    "Type" INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        q.id AS "Id",
        q.name AS "QuizTitle",
        qc.category_name AS "CategoryName",
        qd.name AS "QuizDifficultyLevel",
        q.total_question AS "TotalQuestion",
        q.quiz_type AS "Type"
    FROM "QuizToBaseQuestionMap" qbm
    JOIN "Quiz" q ON q.id = qbm.quiz_id
    JOIN "QuizCategory" qc ON qc.id = q.category_id
    JOIN "QuestionDifficulty" qd ON qd.id = q.difficulty_level_id
    WHERE qbm.que_id = p_question_id
      AND qbm.is_deleted = FALSE
      AND q.is_deleted = FALSE
      AND q.status = 1;
END;
$$;
