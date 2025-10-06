-- =============================================
-- Author:       Vivek Kotadiya
-- Create Date:  06-10-2025
-- Description:  Returns a single paginated question from a given quiz.
--               Includes:
--                 • Question text and type
--                 • Related options in JSON format
--                 • Supports pagination (1 question per page)
--                 • Excludes deleted questions and mappings
--
-- Usage:        SELECT * FROM get_quiz_questions(
--                              p_quiz_id := 1,
--                              p_page_number := 2
--                          );
-- =============================================
CREATE OR REPLACE FUNCTION public.get_quiz_questions(
    p_quiz_id INTEGER,
    p_page_number INTEGER
)
RETURNS TABLE(
    quiz_question_id INTEGER,
    question_name VARCHAR,
    question_type VARCHAR,
    options JSON
)
LANGUAGE 'sql'
COST 100
VOLATILE
PARALLEL UNSAFE
ROWS 1000
AS $BODY$
    SELECT 
        qm.id AS quiz_question_id,                     -- QuizToBaseQuestionMap ID
        que.que_text AS question_name,                 -- Question text
        qt.type_name AS question_type,                 -- Question type name
        COALESCE(
            json_agg(
                json_build_object(
                    'optionId', o.id,
                    'key', o.key,
                    'value', o.value
                )
                ORDER BY o.id
            ) FILTER (WHERE o.id IS NOT NULL),
            '[]'::json
        ) AS options                                   -- List of options in JSON
    FROM "QuizToBaseQuestionMap" qm
    JOIN "BaseQuestions" que 
        ON que.id = qm.que_id
    JOIN "QuestionType" qt 
        ON qt.id = que.que_type_id
    LEFT JOIN "QuestionOptionsAnswers" o 
        ON o.question_id = que.id 
       AND o.key = 'option'
    WHERE qm.quiz_id = p_quiz_id
      AND (qm.is_deleted IS NULL OR qm.is_deleted = FALSE)
    GROUP BY qm.id, que.que_text, qt.type_name
    ORDER BY qm.id
    OFFSET (p_page_number - 1) * 1   -- 1 question per page
    LIMIT 1;
$BODY$;
