-- ==============================================================================
-- Author:       <Brjrajsinh Jadeja>
-- Create date:  <25-September-2025>
-- Description:  <Fetches a single quiz question for an ongoing battle attempt.
--               The function retrieves the question text, type, options,
--               allocated time per question, and XP reward.
--               It supports pagination by returning one question at a time
--               based on the provided page number.
--               Ensures that only active (non-deleted) records are considered.>
-- Usage:        SELECT * FROM get_battle_question(
--                   p_battle_status_id := 101,
--                   p_page_number      := 1
--               );
-- ==============================================================================

CREATE OR REPLACE FUNCTION public.get_battle_question(
    p_battle_status_id integer,
    p_page_number integer
)
RETURNS TABLE(
    quiz_question_id integer,
    question_name varchar,
    question_type varchar,
    options json,
    time_per_question integer,
    xp_per_question integer
)
LANGUAGE sql
AS $$
    SELECT 
        qm.que_id AS quiz_question_id,
        que.que_text AS question_name,
        qt.type_name AS question_type,
        COALESCE(
            json_agg(
                json_build_object(
                    'optionId', o.id,
                    'key', o.key,
                    'value', o.value
                ) ORDER BY o.id
            ) FILTER (WHERE o.id IS NOT NULL),
            '[]'::json
        ) AS options,
        COALESCE(bqd.time_per_question, 30) AS time_per_question,
        COALESCE(qd.xp_gained, 0) AS xp_per_question
    FROM "BattleStatus" bs
    JOIN "BattleList" bl
         ON bl.id = bs.battle_id
    JOIN "QuizToBaseQuestionMap" qm
         ON qm.quiz_id = bl.quiz_id
    JOIN "BaseQuestions" que
         ON que.id = qm.que_id
    JOIN "QuestionType" qt
         ON qt.id = que.que_type_id
    JOIN "QuestionDifficulty" qd
         ON qd.id = que.que_difficulty_id
    LEFT JOIN "QuestionOptionsAnswers" o
         ON o.question_id = que.id AND o.key = 'option'
    LEFT JOIN "BattleQuesDifficultyMap" bqd
         ON bqd.battle_id = bl.id
        AND bqd.que_difficulty_id = que.que_difficulty_id
    WHERE bs.id = p_battle_status_id
      AND COALESCE(bs.is_deleted, FALSE) = FALSE
      AND COALESCE(bl.is_deleted, FALSE) = FALSE
      AND COALESCE(qm.is_deleted, FALSE) = FALSE
      AND COALESCE(que.is_deleted, FALSE) = FALSE
    GROUP BY qm.id, que.que_text, qt.type_name,
             bqd.time_per_question, qd.xp_gained
    ORDER BY qm.id
    OFFSET (p_page_number - 1)
    LIMIT 1;
$$;
