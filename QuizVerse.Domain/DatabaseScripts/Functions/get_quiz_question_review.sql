-- =============================================
-- Author:      Zeel Vaghasiya
-- Create date: 11-September-2025
-- Description: Retrieves the list of questions for a given quiz and user.
--              Returns user answers, correct answers, and correctness status.
--              Only includes questions that existed at the time the user started 
--              the quiz, preventing newly added questions from appearing.
-- Usage:       SELECT * FROM get_quiz_question_review(p_quiz_id, p_user_id);
-- Example:     SELECT * FROM get_quiz_question_review(101, 202);
-- =============================================

CREATE OR REPLACE FUNCTION public.get_quiz_question_review(
    p_quiz_id INT,
    p_user_id INT
) 
RETURNS TABLE (
    question_id INT,
    question_text VARCHAR,
    user_answer VARCHAR,
    correct_answer VARCHAR,
    is_correct BOOLEAN
) 
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        bq.id AS question_id,
        bq.que_text AS question_text,
        a.given_answer::VARCHAR AS user_answer,
        ca.correct_answer::VARCHAR AS correct_answer,
        a.is_correct
    FROM public."QuizToBaseQuestionMap" qbm
    INNER JOIN public."BaseQuestions" bq 
        ON qbm.que_id = bq.id
    LEFT JOIN public."QuizPlayStatus" qps 
        ON qps.quiz_id = qbm.quiz_id 
       AND qps.user_id = p_user_id
    LEFT JOIN public."AttemptedQuizQuestionsAnswer" a 
        ON a.quiz_play_status_id = qps.id
       AND a.quiz_que_id = qbm.id
    LEFT JOIN (
        SELECT DISTINCT ON (qoa.question_id) 
               qoa.question_id AS ca_question_id,
               qoa.value AS correct_answer
        FROM public."QuestionOptionsAnswers" qoa
        WHERE qoa.key = 'answer' AND qoa.is_deleted = false
        ORDER BY qoa.question_id, qoa.id ASC
    ) ca ON ca.ca_question_id = bq.id
    WHERE qbm.quiz_id = p_quiz_id
      AND qbm.is_deleted = false
      AND bq.is_deleted = false;
      AND qbm.created_date <= qps.created_date;
END;
$$ LANGUAGE plpgsql STABLE;