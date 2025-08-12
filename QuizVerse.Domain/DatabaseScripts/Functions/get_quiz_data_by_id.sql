-- =============================================
-- Author:       Darsh Aswani
-- Create Date:  12-August-2025
-- Description:  Retrieves full quiz data by quiz ID with separate SQL columns:
--                 • Quiz details: id, name, quiz_category_id, description, total_time,
--                   difficulty_level_id, total_question, is_paid, price, status
--                 • tags (JSONB array of { id, name })
--                 • questions (JSONB array of { id, category_id, que_difficulty_id, que_text,
--                   que_type_id, que_options_ans[] })
--                     - que_options_ans[] is JSONB array of { id, question_id, key, value }
-- Returns:       One row with scalar columns + nested JSONB arrays for tags and questions
-- Usage:         SELECT * FROM get_quiz_data_by_id(18);
-- =============================================

CREATE OR REPLACE FUNCTION get_quiz_data_by_id(p_quiz_id INT)
RETURNS TABLE (
    id INT,
    name VARCHAR,
    quiz_category_id INT,
    description VARCHAR,
    total_time INT,
    difficulty_level_id INT,
    total_question INT,
    is_paid BOOLEAN,
    price NUMERIC,
    status INT,
    tags JSONB,
    questions JSONB
) AS
$$
BEGIN
    RETURN QUERY
    SELECT
        q.id,
        q.name,
        q.category_id AS quiz_category_id,
        q.description,
        q.total_time,
        q.difficulty_level_id,
        q.total_question,
        q.is_paid,
        q.price,
        q.status,
        COALESCE(
            (
                SELECT jsonb_agg(
                    jsonb_build_object(
                        'id', t.id,
                        'name', t.tag_name
                    )
                )
                FROM "QuizTagMapping" qtm
                JOIN "QuizTag" t ON t.id = qtm.tag_id
                WHERE qtm.quiz_id = q.id
            ), '[]'::jsonb
        ) AS tags,
        COALESCE(
            (
                SELECT jsonb_agg(
                    jsonb_build_object(
                        'id', bq.id,
                        'category_id', bq.category_id,
                        'que_difficulty_id', bq.que_difficulty_id,
                        'que_text', bq.que_text,
                        'que_type_id', bq.que_type_id,
                        'que_options_ans', COALESCE(
                            (
                                SELECT jsonb_agg(
                                    jsonb_build_object(
                                        'id', qo.id,
                                        'question_id', qo.question_id,
                                        'key', qo.key,
                                        'value', qo.value
                                    )
                                )
                                FROM "QuestionOptionsAnswers" qo
                                WHERE qo.question_id = bq.id
                                  AND qo.is_deleted = FALSE
                            ), '[]'::jsonb
                        )
                    )
                )
                FROM "QuizToBaseQuestionMap" qbm
                JOIN "BaseQuestions" bq ON bq.id = qbm.que_id
                WHERE qbm.quiz_id = q.id
                  AND bq.is_deleted = FALSE
            ), '[]'::jsonb
        ) AS questions
    FROM "Quiz" q
    WHERE q.id = p_quiz_id
      AND q.is_deleted = FALSE;
END;
$$ LANGUAGE plpgsql;
