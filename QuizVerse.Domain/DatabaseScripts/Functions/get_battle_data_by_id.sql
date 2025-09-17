-- =============================================
-- Author:       Bhumi shah
-- Create Date:  25-August-2025
-- Description:  Fetches complete battle data by its ID, including:
--                 • Battle metadata from "BattleList" and "Quiz"
--                 • Battle type (time-limited or normal)
--                 • Quiz details: total_time, total_questions, total_xp, difficulty, status, category
--                 • Full questions JSON (with options) linked to the quiz
--                 • Question difficulty JSON (per difficulty mapping)
--                 • Always returns empty JSON arrays ([]) instead of NULL for questions/difficulties
-- Usage:        SELECT * FROM get_battle_data_by_id(5);
-- =============================================

CREATE OR REPLACE FUNCTION get_battle_data_by_id(p_battle_id INT)
RETURNS TABLE (
    "Id" INT,
    "Name" VARCHAR,
    "Description" VARCHAR,
    "BattleType" INT,
    "StartDate" TIMESTAMPTZ,
    "EndDate" TIMESTAMPTZ,
    "TotalTime" NUMERIC,
    "TotalQuestion" INT,
    "TotalXp" INT,
    "Status" INT,
    "DifficultyLevelId" INT,
    "CategoryId" INT,
    "QuestionsJson" JSONB,
    "QuestionsDifficultyJson" JSONB
) 
LANGUAGE plpgsql
AS $$
BEGIN
    -- First check if quiz exists
    IF NOT EXISTS (
        SELECT 1
        FROM "BattleList" b
        WHERE b.id = p_battle_id
        AND b.is_deleted = FALSE
    ) THEN
        RAISE EXCEPTION 'Battle not found.'
            USING ERRCODE = 'P0001';
    END IF;

	-- Checks if quiz is being played right now
	IF EXISTS (
		SELECT 1 
		FROM "BattleStatus" bs
		where bs.battle_id = p_battle_id AND bs.battle_status = 3
	) THEN 
        RAISE EXCEPTION 'Someone is playing this battle currently. So you can not edit it.'
            USING ERRCODE = 'P0001';
	END IF;
    
    RETURN QUERY
    SELECT
        b.id AS "Id",
        q.name AS "Name",
        q.description AS "Description",
        CASE WHEN b.battle_time_limited = TRUE THEN 2 ELSE 1 END AS "BattleType",
        b.start_date AS "StartDate",
        b.end_date AS "EndDate",
        q.total_time AS "TotalTime",
        q.total_question AS "TotalQuestion",
        q.total_xp AS "TotalXp",
        q.status AS "Status",
        q.difficulty_level_id AS "DifficultyLevelId",
        q.category_id AS "CategoryId",

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
                                        'id', o.id,
                                        'questionId', o.question_id,
                                        'key', o.key,
                                        'value', o.value
                                    )
                                )
                                FROM "QuestionOptionsAnswers" o
                                WHERE o.question_id = bq.id
                                  AND o.is_deleted = FALSE
                            ), '[]'::jsonb
                        )
                    )
                )
                FROM "BaseQuestions" bq
                JOIN "QuizToBaseQuestionMap" qm ON qm.que_id = bq.id
                WHERE qm.quiz_id = q.id
                  AND bq.is_deleted = FALSE
                  AND qm.is_deleted = FALSE
            ), '[]'::jsonb
        ) AS "QuestionsJson",

        COALESCE(
            (
                SELECT jsonb_agg(
                    jsonb_build_object(
                        'id', qdmap.id,
                        'queDifficultyId', qdmap.que_difficulty_id,
                        'noOfQues', qdmap.no_of_ques,
                        'timePerQuestion', qdmap.time_per_question
                    )
                )
                FROM "BattleQuesDifficultyMap" qdmap
                WHERE qdmap.battle_id = b.id
                  AND qdmap.is_deleted = FALSE
            ), '[]'::jsonb
        ) AS "QuestionsDifficultyJson"

    FROM "BattleList" b
    JOIN "Quiz" q ON q.id = b.quiz_id
    WHERE b.id = p_battle_id
      AND b.is_deleted = FALSE
      AND q.is_deleted = FALSE;
END;
$$;