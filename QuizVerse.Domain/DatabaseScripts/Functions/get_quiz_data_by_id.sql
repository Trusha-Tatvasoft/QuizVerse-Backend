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
    "quizCategoryId" INT,
    description VARCHAR,
    "totalTime" INT,
    "difficultyLevelId" INT,
    "totalQuestion" INT,
    "isPaid" BOOLEAN,
    price NUMERIC,
    status INT,
    tags JSONB,
    questions JSONB,
    "noOfQuestionsPerDifficulty" JSONB
) AS
$$
BEGIN
    RETURN QUERY
    SELECT
        q.id,
        q.name,
        q.category_id AS "quizCategoryId",
        q.description,
        q.total_time AS "totalTime",
        q.difficulty_level_id AS "difficultyLevelId",
        q.total_question AS "totalQuestion",
        q.is_paid AS "isPaid",
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
                        'categoryId', bq.category_id,
                        'queDifficultyId', bq.que_difficulty_id,
                        'queText', bq.que_text,
                        'queTypeId', bq.que_type_id,
                        'queOptionsAns', COALESCE(
                            (
                                SELECT jsonb_agg(
                                    jsonb_build_object(
                                        'id', qo.id,
                                        'questionId', qo.question_id,
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
        ) AS questions,
        COALESCE(
            (
                SELECT jsonb_agg(
                    jsonb_build_object(
                        'queDifficultyName', qd.name,
                        'noOfQuestions', qmap.no_of_questions
                    )
                )
                FROM "QuizToQuestionDifficultyMap" qmap
                JOIN "QuestionDifficulty" qd 
                    ON qd.id = qmap.question_difficulty_id
                WHERE qmap.quiz_id = q.id
                AND qmap.is_deleted = FALSE
                AND qd.is_deleted = FALSE
            ),
            '[]'::jsonb
        ) AS "noOfQuestionsPerDifficulty"
    FROM "Quiz" q
    WHERE q.id = p_quiz_id
      AND q.is_deleted = FALSE;
END;
$$ LANGUAGE plpgsql;
