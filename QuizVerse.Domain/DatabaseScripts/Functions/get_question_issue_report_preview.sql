-- =============================================
-- Author:       Bhumi Shah
-- Create Date:  04-Nov-2025
-- Description:  Returns a preview of how many active quizzes and battles contain a specific question
-- Usage: SELECT * FROM get_question_issue_report_preview(103);
-- =============================================

CREATE OR REPLACE FUNCTION get_question_issue_report_preview(
    p_question_id INT
)
RETURNS TABLE (
    "ActiveQuizContainCount" INT,
    "ActiveBattleContainCount" INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        -- Count of active normal quizzes containing this question
        COALESCE((
            SELECT COUNT(DISTINCT qbm.quiz_id)
            FROM "QuizToBaseQuestionMap" qbm
            JOIN "Quiz" q ON q.id = qbm.quiz_id
            WHERE qbm.que_id = p_question_id
              AND q.is_deleted = FALSE
			  AND qbm.is_deleted = FALSE
              AND q.status = 1
              AND q.quiz_type = 1
        ), 0)::INT AS "ActiveQuizContainCount",

        -- Count of active battle quizzes containing this question
        COALESCE((
            SELECT COUNT(DISTINCT qbm.quiz_id)
            FROM "QuizToBaseQuestionMap" qbm
            JOIN "Quiz" q ON q.id = qbm.quiz_id
            WHERE qbm.que_id = p_question_id
              AND q.is_deleted = FALSE 
              AND qbm.is_deleted = FALSE   
              AND q.status = 1
              AND q.quiz_type = 2
        ), 0)::INT AS "ActiveBattleContainCount";
END;
$$;
