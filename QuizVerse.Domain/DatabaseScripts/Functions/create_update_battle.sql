-- =============================================
-- Author:       Bhumi Shah
-- Create Date:  25-August-2025
-- Description:  Creates or updates a battle with full metadata, including:
--                 • Inserts or updates quiz details in "Quiz" table
--                 • Inserts or updates battle details in "BattleList"
--                 • Handles difficulty mappings:
--                     - Updates existing mappings if present
--                     - Inserts new mappings if missing
--                     - Marks unmapped difficulty entries as deleted during update
--                 • Handles questions:
--                     - Uses existing questions if ID is provided
--                     - Inserts new questions into "BaseQuestions" if ID is NULL
--                     - Inserts new options into "QuestionOptionsAnswers" for new questions
--                     - Creates or updates mappings in "QuizToBaseQuestionMap"
--                     - Marks unmapped questions as deleted during update
--                 • Automatically assigns 'created_by' or 'modified_by' for all changes
--                 • Returns success status and message
-- Usage (CREATE): SELECT * FROM create_update_battle(
--                     p_battle_id           := NULL,
--                     p_name                := 'Demo Battle',
--                     p_description         := 'A fun mixed battle',
--                     p_difficulty_level_id := 2,
--                     p_category_id         := 1,
--                     p_status              := 1,
--                     p_is_time_limited     := TRUE,
--                     p_start_date          := NOW(),
--                     p_end_date            := NOW() + interval '7 days',
--                     p_total_time          := 90,
--                     p_total_question      := 20,
--                     p_total_xp            := 200,
--                     p_questions           := '[]'::jsonb,
--                     p_question_difficulty := '[]'::jsonb,
--                     p_quiz_types          := 2,
--                     p_created_by          := 1
--                 );
--
-- Usage (UPDATE): SELECT * FROM create_update_battle(
--                     p_battle_id           := 13,
--                     p_name                := 'Demo Battle Updated',
--                     p_description         := 'Updated description',
--                     p_difficulty_level_id := 2,
--                     p_category_id         := 1,
--                     p_status              := 1,
--                     p_is_time_limited     := TRUE,
--                     p_start_date          := NOW(),
--                     p_end_date            := NOW() + interval '10 days',
--                     p_total_time          := 100,
--                     p_total_question      := 25,
--                     p_total_xp            := 250,
--                     p_questions           := '[{ "id":126, "categoryId":1, "queDifficultyId":2, "queText":"Updated Q", "queTypeId":1, "queOptionsAns":[] }]'::jsonb,
--                     p_question_difficulty := '[{ "queDifficultyId":2, "noOfQues":15, "timePerQuestion":45 }]'::jsonb,
--                     p_quiz_types          := 2,
--                     p_created_by          := 1
--                 );
-- =============================================

CREATE OR REPLACE FUNCTION create_update_battle(
    p_battle_id INT,
    p_name TEXT,
    p_description TEXT,
    p_difficulty_level_id INT,
    p_category_id INT,
    p_status INT,
    p_is_time_limited BOOLEAN,
    p_start_date TIMESTAMP,
    p_end_date TIMESTAMP,
    p_total_time INT,
    p_total_question INT,
    p_total_xp INT,
    p_questions JSONB,
    p_question_difficulty JSONB,
    p_quiz_types INT,
    p_created_by INT
)
RETURNS TABLE (p_success BOOLEAN, p_message TEXT)
LANGUAGE plpgsql
AS $$
DECLARE
    v_quiz_id INT;
    v_exists INT;
    v_question_id INT;
    v_opt RECORD;
    v_q RECORD;
    v_mode TEXT;
BEGIN
    IF p_battle_id IS NOT NULL THEN
        SELECT COUNT(*) INTO v_exists
        FROM "BattleList" b
        WHERE b.id = p_battle_id AND b.is_deleted = FALSE;

        IF v_exists = 0 THEN
            RETURN QUERY SELECT FALSE, 'Battle not found';
            RETURN;
        END IF;

        SELECT COUNT(*) INTO v_exists
        FROM "Quiz" q
        JOIN "BattleList" b ON b.quiz_id = q.id
        WHERE LOWER(TRIM(q.name)) = LOWER(TRIM(p_name))
          AND q.is_deleted = FALSE
          AND b.id <> p_battle_id;

        IF v_exists > 0 THEN
            RETURN QUERY SELECT FALSE, 'Battle with the same name already exists';
            RETURN;
        END IF;

        UPDATE "Quiz"
        SET name                = p_name,
            description         = p_description,
            total_time          = p_total_time,
            total_question      = p_total_question,
            total_xp            = p_total_xp,
            status              = p_status,
            difficulty_level_id = p_difficulty_level_id,
            category_id         = p_category_id,
            quiz_type           = p_quiz_types,
            modified_by         = p_created_by,
            modified_date       = NOW()
        WHERE id = (SELECT quiz_id FROM "BattleList" WHERE id = p_battle_id)
        RETURNING id INTO v_quiz_id;

        UPDATE "BattleList"
        SET start_date          = p_start_date,
            end_date            = p_end_date,
            battle_time_limited = p_is_time_limited,
            modified_by         = p_created_by,
            modified_date       = NOW()
        WHERE id = p_battle_id;

        UPDATE "BattleQuesDifficultyMap"
        SET is_deleted = TRUE, modified_by = p_created_by, modified_date = NOW()
        WHERE battle_id = p_battle_id
          AND (
              p_question_difficulty IS NULL
              OR que_difficulty_id NOT IN (
                    SELECT (elem->>'queDifficultyId')::INT
                    FROM jsonb_array_elements(COALESCE(p_question_difficulty, '[]'::jsonb)) elem
              )
          );

        UPDATE "QuizToBaseQuestionMap"
        SET is_deleted = TRUE, modified_by = p_created_by, modified_date = NOW()
        WHERE quiz_id = v_quiz_id
          AND (
              p_questions IS NULL
              OR que_id NOT IN (
                    SELECT (q->>'id')::INT
                    FROM jsonb_array_elements(COALESCE(p_questions, '[]'::jsonb)) q
                    WHERE q->>'id' IS NOT NULL
              )
          );

        v_mode := 'update';
    ELSE
        SELECT COUNT(*) INTO v_exists
        FROM "Quiz" q
        WHERE LOWER(TRIM(q.name)) = LOWER(TRIM(p_name))
          AND q.is_deleted = FALSE;

        IF v_exists > 0 THEN
            RETURN QUERY SELECT FALSE, 'Battle with the same name already exists';
            RETURN;
        END IF;

        INSERT INTO "Quiz" (
            name, description, total_time, total_question, total_xp,
            status, difficulty_level_id, category_id, created_by, quiz_type
        )
        VALUES (
            p_name, p_description, p_total_time, p_total_question, p_total_xp,
            p_status, p_difficulty_level_id, p_category_id, p_created_by, p_quiz_types
        )
        RETURNING id INTO v_quiz_id;

        INSERT INTO "BattleList" (
            start_date, end_date, quiz_id, created_by, battle_time_limited
        )
        VALUES (
            p_start_date, p_end_date, v_quiz_id, p_created_by, p_is_time_limited
        )
        RETURNING id INTO p_battle_id;

        v_mode := 'create';
    END IF;

    IF p_question_difficulty IS NOT NULL AND jsonb_array_length(p_question_difficulty) > 0 THEN
        FOR v_q IN
            SELECT * FROM jsonb_to_recordset(p_question_difficulty)
            AS t("queDifficultyId" INT, "noOfQues" INT, "timePerQuestion" INT)
        LOOP
            UPDATE "BattleQuesDifficultyMap"
            SET no_of_ques       = v_q."noOfQues",
                time_per_question = v_q."timePerQuestion",
                modified_by      = p_created_by,
                modified_date    = NOW(),
                is_deleted       = FALSE
            WHERE battle_id = p_battle_id
              AND que_difficulty_id = v_q."queDifficultyId";

            IF NOT FOUND THEN
                INSERT INTO "BattleQuesDifficultyMap" (
                    battle_id, que_difficulty_id, no_of_ques, time_per_question, created_by
                )
                VALUES (
                    p_battle_id, v_q."queDifficultyId", v_q."noOfQues", v_q."timePerQuestion", p_created_by
                );
            END IF;
        END LOOP;
    END IF;

    IF p_questions IS NOT NULL AND jsonb_array_length(p_questions) > 0 THEN
        FOR v_q IN
            SELECT *
            FROM jsonb_to_recordset(p_questions)
            AS q(id INT, "categoryId" INT, "queDifficultyId" INT, "queText" TEXT, "queTypeId" INT, "queOptionsAns" JSONB)
        LOOP
            IF v_q.id IS NULL THEN
                INSERT INTO "BaseQuestions" (
                    category_id, que_difficulty_id, que_text, que_type_id, created_by
                )
                VALUES (
                    v_q."categoryId", v_q."queDifficultyId", v_q."queText", v_q."queTypeId", p_created_by
                )
                RETURNING id INTO v_question_id;

                IF v_q."queOptionsAns" IS NOT NULL AND jsonb_array_length(v_q."queOptionsAns") > 0 THEN
                    FOR v_opt IN
                        SELECT * FROM jsonb_to_recordset(v_q."queOptionsAns") AS o(id INT, key TEXT, value TEXT)
                    LOOP
                        INSERT INTO "QuestionOptionsAnswers"(question_id, key, value, created_by)
                        VALUES (v_question_id, v_opt.key, v_opt.value, p_created_by);
                    END LOOP;
                END IF;
            ELSE
                v_question_id := v_q.id;
            END IF;

            UPDATE "QuizToBaseQuestionMap"
            SET is_deleted   = FALSE,
                modified_by  = p_created_by,
                modified_date= NOW()
            WHERE quiz_id = v_quiz_id
              AND que_id  = v_question_id;

            IF NOT FOUND THEN
                INSERT INTO "QuizToBaseQuestionMap"(quiz_id, que_id, created_by)
                VALUES (v_quiz_id, v_question_id, p_created_by);
            END IF;
        END LOOP;
    END IF;

    IF v_mode = 'create' THEN
        RETURN QUERY SELECT TRUE, 'Battle created successfully';
    ELSE
        RETURN QUERY SELECT TRUE, 'Battle updated successfully';
    END IF;
END;
$$;