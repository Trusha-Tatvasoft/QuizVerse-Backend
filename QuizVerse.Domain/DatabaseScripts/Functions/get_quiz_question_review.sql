-- =============================================
-- Author:      Zeel Vaghasiya
-- Create date: 11-September-2025
-- Updated by:  Darsh Aswani
-- Updated on:  07-October-2025
-- Description: Retrieves all quiz questions for a given user attempt.
--              Includes:
--                - Attempted questions (even if deleted/removed later)
--                - Questions that existed at the time of the attempt
--              Excludes:
--                - Questions deleted/removed before attempt
--                - Questions added after attempt
-- Usage:       SELECT * FROM get_quiz_question_review(p_quiz_id, p_user_id);
-- Example:     SELECT * FROM get_quiz_question_review(9, 113);
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
    WITH qps AS (
        SELECT * FROM public."QuizPlayStatus"
        WHERE quiz_id = p_quiz_id AND user_id = p_user_id
        LIMIT 1
    ),
    attempted AS (
        SELECT 
            a.quiz_que_id,
            bq.id AS base_question_id,
            COALESCE(NULLIF(a.question_text, ''), bq.que_text)::VARCHAR AS question_text,
            a.given_answer,
            a.is_correct,
            COALESCE(NULLIF(a.question_answer, ''), ca.correct_answer)::VARCHAR AS correct_answer
        FROM public."AttemptedQuizQuestionsAnswer" a
        INNER JOIN qps ON a.quiz_play_status_id = qps.id
        LEFT JOIN public."QuizToBaseQuestionMap" qbm ON a.quiz_que_id = qbm.id
        LEFT JOIN public."BaseQuestions" bq ON qbm.que_id = bq.id
        LEFT JOIN (
            SELECT DISTINCT ON (qoa.question_id) 
                   qoa.question_id, qoa.value AS correct_answer
            FROM public."QuestionOptionsAnswers" qoa
            WHERE qoa.key = 'answer'
            ORDER BY qoa.question_id, qoa.id ASC
        ) ca ON ca.question_id = bq.id
    ),
    historical_map AS (
        -- Include questions that were in the map when attempt started
        SELECT 
            qbm.id AS quiz_que_id,
            bq.id AS base_question_id,
            bq.que_text AS question_text,
            NULL AS given_answer,
            NULL AS is_correct,
            ca.correct_answer
        FROM public."QuizToBaseQuestionMap" qbm
        INNER JOIN public."BaseQuestions" bq ON qbm.que_id = bq.id
        INNER JOIN qps ON TRUE
        LEFT JOIN (
            SELECT DISTINCT ON (qoa.question_id) 
                   qoa.question_id, qoa.value AS correct_answer
            FROM public."QuestionOptionsAnswers" qoa
            WHERE qoa.key = 'answer' AND qoa.is_deleted = false
            ORDER BY qoa.question_id, qoa.id ASC
        ) ca ON ca.question_id = bq.id
        WHERE qbm.quiz_id = p_quiz_id
          AND qbm.created_date <= qps.created_date
          AND (
		    (qbm.is_deleted = false AND qbm.modified_date::timestamp <= qps.modified_date::timestamp)
		    OR (qbm.is_deleted = true AND qbm.modified_date::timestamp >= qps.created_date::timestamp)
		)
    )
    SELECT 
        COALESCE(attempted.base_question_id, historical_map.base_question_id) AS question_id,
        COALESCE(attempted.question_text, historical_map.question_text)::VARCHAR AS question_text,
        attempted.given_answer::VARCHAR AS user_answer,
        COALESCE(attempted.correct_answer, historical_map.correct_answer)::VARCHAR AS correct_answer,
        attempted.is_correct
    FROM attempted
    FULL OUTER JOIN historical_map 
        ON attempted.base_question_id = historical_map.base_question_id
    ORDER BY question_id ASC;
END;
$$ LANGUAGE plpgsql STABLE;
