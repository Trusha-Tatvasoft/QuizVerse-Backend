-- =============================================
-- Author:       Darsh Aswani
-- Create Date:  08-August-2025
-- Description:  Creates or updates a quiz with full metadata, including:
--                 • Inserting or updating quiz details in "Quiz" table
--                 • Handling tags:
--                     - Uses existing tags if ID is provided
--                     - Inserts new tags if ID is NULL
--                     - Creates mapping entries in "QuizTagMapping"
--                     - Removes unmapped tags during update
--                 • Handling questions:
--                     - Uses existing questions if ID is provided
--                     - Inserts new questions into "BaseQuestions" if ID is NULL
--                     - Inserts new question options into "QuestionOptionsAnswers" for new questions
--                     - Creates mapping entries in "QuizToBaseQuestionMap"
--                     - Removes unmapped questions during update
--                 • Calculates and updates total_xp based on question difficulty
--                 • Marks quiz as 'featured' if its category was created in the last 30 days
--                 • Automatically assigns 'created_by' or 'modified_by' for all changes
--                 • Returns success status and message
-- Usage (CREATE): SELECT * FROM create_update_quiz(
--                     p_quiz_id             := NULL,   -- NULL for create
--                     p_name                := 'Draft Quiz 3',
--                     p_category_id         := 1,
--                     p_description         := 'A fun quiz.',
--                     p_total_time          := 30,
--                     p_difficulty_level_id := 2,
--                     p_total_question      := 10,
--                     p_is_paid             := FALSE,
--                     p_price               := NULL,
--                     p_status              := 2,
--                     p_tags                := '[]'::jsonb,
--                     p_questions           := '[]'::jsonb,
--                     p_created_by          := 1
--                 );
--
-- Usage (UPDATE): SELECT * FROM create_update_quiz(
--                     p_quiz_id             := 11,      -- Existing quiz ID
--                     p_name                := 'Draft Quizes',
--                     p_category_id         := 1,
--                     p_description         := 'An updated description.',
--                     p_total_time          := 30,
--                     p_difficulty_level_id := 2,
--                     p_total_question      := 10,
--                     p_is_paid             := TRUE,
--                     p_price               := 9.99,
--                     p_status              := 2,
--                     p_tags                := '[{ "id": null, "name": "Geography" }]'::jsonb,
--                     p_questions           := '[]'::jsonb,
--                     p_no_of_questions_per_difficulty := '[{"queDifficultyName": "easy", "noOfQuestions": 4}]'::jsonb,
--                     p_created_by          := 1
--                 );
-- =============================================

CREATE OR REPLACE FUNCTION create_update_quiz(
    p_quiz_id INT,          -- NULL for create, non-null for update
    p_name TEXT,
    p_category_id INT,
    p_description TEXT,
    p_total_time INT,
    p_difficulty_level_id INT,
    p_total_question INT,
    p_is_paid BOOLEAN,
    p_price NUMERIC(10,2),
    p_status INT,
    p_tags JSONB,         -- [{ id?, name }]
    p_questions JSONB,    -- [{ id?, categoryId, queDifficultyId, queText, queTypeId, queOptionsAns }]
    p_no_of_questions_per_difficulty JSONB, -- [{ queQifficultyName, noOfQuestions }]
    p_created_by INT
)
RETURNS TABLE (
    p_success BOOLEAN,
    p_message TEXT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_tag RECORD;
    v_q RECORD;
    v_opt RECORD;
    v_tag_id INT;
    v_quiz_id INT;
    v_question_id INT;
    v_difficulty_id INT;
    v_total_xp INT := 0;
    v_xp INT;
    v_is_recent_category INT := 0;
    v_is_featured BOOLEAN := FALSE;
BEGIN
    -- 1. Validation & Duplicate Name Check
    IF p_quiz_id IS NULL THEN
        -- CREATE mode: check name for all quizzes
        IF EXISTS (
            SELECT 1 FROM "Quiz"
            WHERE LOWER(name) = LOWER(TRIM(p_name))
              AND is_deleted = FALSE
        ) THEN
            RETURN QUERY SELECT FALSE, 'A quiz with the same name already exists.';
            RETURN;
        END IF;
    ELSE
        -- UPDATE mode: check quiz exists
        IF NOT EXISTS (
            SELECT 1 FROM "Quiz"
            WHERE id = p_quiz_id AND is_deleted = FALSE
        ) THEN
            RETURN QUERY SELECT FALSE, 'Quiz not found.';
            RETURN;
        END IF;

        -- Duplicate name excluding current quiz
        IF EXISTS (
            SELECT 1 FROM "Quiz"
            WHERE LOWER(name) = LOWER(TRIM(p_name))
              AND id <> p_quiz_id
              AND is_deleted = FALSE
        ) THEN
            RETURN QUERY SELECT FALSE, 'Another quiz with the same name already exists.';
            RETURN;
        END IF;
    END IF;

    -- 2. Calculate total XP
    IF p_questions IS NOT NULL AND jsonb_array_length(p_questions) > 0 THEN
        FOR v_q IN
            SELECT * FROM jsonb_to_recordset(p_questions)
            AS q(id INT, "categoryId" INT, "queDifficultyId" INT, "queText" TEXT, "queTypeId" INT, "queOptionsAns" JSONB)
        LOOP
            SELECT xp_gained
            INTO v_xp
            FROM "QuestionDifficulty"
            WHERE id = v_q."queDifficultyId"
              AND is_deleted = FALSE;

            v_total_xp := v_total_xp + COALESCE(v_xp, 0);
        END LOOP;
    END IF;

    -- 3. Check if category is recent (last 30 days)
    SELECT 1
    INTO v_is_recent_category
    FROM "QuizCategory"
    WHERE is_deleted = FALSE
      AND created_date >= CURRENT_TIMESTAMP - INTERVAL '30 days' 
      AND id = p_category_id
    LIMIT 1;

    v_is_featured := (v_is_recent_category = 1);

    -- 4. Create or Update Quiz record
    IF p_quiz_id IS NULL THEN
        -- CREATE
        INSERT INTO "Quiz" (
            name, category_id, description, total_time, difficulty_level_id,
            total_question, is_paid, price, status, created_by, total_xp, is_featured
        )
        VALUES (
            p_name, p_category_id, p_description, p_total_time, p_difficulty_level_id,
            p_total_question, p_is_paid, COALESCE(p_price, 0), p_status, p_created_by, v_total_xp, v_is_featured
        )
        RETURNING id INTO v_quiz_id;
    ELSE
        -- UPDATE
        UPDATE "Quiz"
        SET name = p_name,
            category_id = p_category_id,
            description = p_description,
            total_time = p_total_time,
            difficulty_level_id = p_difficulty_level_id,
            total_question = p_total_question,
            is_paid = p_is_paid,
            price = COALESCE(p_price, 0),
            status = p_status,
            total_xp = v_total_xp,
            is_featured = v_is_featured,
            modified_by = p_created_by,
            modified_date = NOW()
        WHERE id = p_quiz_id;

        v_quiz_id := p_quiz_id;

        -- Remove unmapped tags
        Update "QuizTagMapping" 
        SET modified_by = p_created_by, modified_date = CURRENT_TIMESTAMP, 
            is_deleted = TRUE
        WHERE quiz_id = v_quiz_id
          AND tag_id NOT IN (
              SELECT COALESCE(t.id, qt.id)
              FROM jsonb_to_recordset(p_tags) AS t(id INT, name TEXT)
              LEFT JOIN "QuizTag" qt ON LOWER(qt.tag_name) = LOWER(TRIM(t.name))
          );
        -- Remove unmapped questions
        Update "QuizToBaseQuestionMap" 
        SET modified_by = p_created_by, modified_date = CURRENT_TIMESTAMP, 
            is_deleted = TRUE
        WHERE quiz_id = v_quiz_id
        AND que_id NOT IN (
            SELECT id FROM jsonb_to_recordset(p_questions) AS q(id INT, "categoryId" INT, "queDifficultyId" INT, "queText" TEXT, "queTypeId" INT, "queOptionsAns" JSONB)
            WHERE id IS NOT NULL
        );
    END IF;

    -- 5. Handle Mapping No of Questions Per Difficulty
    IF p_no_of_questions_per_difficulty IS NOT NULL
    AND jsonb_array_length(p_no_of_questions_per_difficulty) > 0
    THEN
        -- Upsert (update/insert) for JSON entries
        FOR v_q IN
            SELECT * 
            FROM jsonb_to_recordset(p_no_of_questions_per_difficulty)
            AS q("queDifficultyName" TEXT, "noOfQuestions" INT)
        LOOP
            RAISE NOTICE 'Difficulty Name: "%", No Of Questions: %', 
                        v_q."queDifficultyName", v_q."noOfQuestions";

            -- Look up difficultyId by name
            SELECT id 
            INTO v_difficulty_id
            FROM "QuestionDifficulty"
            WHERE LOWER(name) = LOWER(TRIM(v_q."queDifficultyName"))
            AND is_deleted = FALSE
            LIMIT 1;

            -- If not found → throw error (business rule)
            IF v_difficulty_id IS NULL THEN
                RAISE NOTICE 'Difficulty "%" not found.', v_q."queDifficultyName";
                RETURN QUERY SELECT FALSE, 'Invalid difficulty name provided.';
                RETURN;
            END IF;

            -- Try to update existing record
            UPDATE "QuizToQuestionDifficultyMap"
            SET no_of_questions = v_q."noOfQuestions",
                modified_by     = p_created_by,
                modified_date   = NOW(),
                is_deleted      = FALSE
            WHERE quiz_id = v_quiz_id
            AND question_difficulty_id = v_difficulty_id;

            -- If nothing updated, insert new record
            IF NOT FOUND THEN
                INSERT INTO "QuizToQuestionDifficultyMap"
                    (quiz_id, question_difficulty_id, no_of_questions, is_deleted, created_by, created_date)
                VALUES
                    (v_quiz_id, v_difficulty_id, v_q."noOfQuestions", FALSE, p_created_by, NOW());
            END IF;
        END LOOP;

        -- Soft delete rows that are NOT in JSON
        UPDATE "QuizToQuestionDifficultyMap"
        SET is_deleted    = TRUE,
            modified_by   = p_created_by,
            modified_date = NOW()
        WHERE quiz_id = v_quiz_id
        AND question_difficulty_id NOT IN (
            SELECT qd.id
            FROM jsonb_to_recordset(p_no_of_questions_per_difficulty)
                AS q("queDifficultyName" TEXT, "noOfQuestions" INT)
            JOIN "QuestionDifficulty" qd 
                ON LOWER(qd.name) = LOWER(TRIM(q."queDifficultyName"))
            AND qd.is_deleted = FALSE
        )
        AND is_deleted = FALSE;
    END IF;

    -- 6. Handle Tags
    FOR v_tag IN
        SELECT * FROM jsonb_to_recordset(p_tags) AS t(id INT, name TEXT)
    LOOP
        IF v_tag.id IS NULL THEN
            SELECT id INTO v_tag_id
            FROM "QuizTag"
            WHERE LOWER(tag_name) = LOWER(TRIM(v_tag.name))
            LIMIT 1;

            IF v_tag_id IS NULL THEN
                INSERT INTO "QuizTag" (tag_name, created_by)
                VALUES (v_tag.name, p_created_by)
                RETURNING id INTO v_tag_id;
            END IF;
        ELSE
            v_tag_id := v_tag.id;
        END IF;

       -- Update existing records where is_deleted = TRUE to set is_deleted = FALSE
        UPDATE "QuizTagMapping"
        SET is_deleted = FALSE, modified_by = p_created_by, modified_date = CURRENT_TIMESTAMP
        WHERE quiz_id = v_quiz_id AND tag_id = v_tag_id AND is_deleted = TRUE;

        -- Insert new records only if no matching record exists (active or deleted)
        INSERT INTO "QuizTagMapping" (quiz_id, tag_id, created_by)
        SELECT v_quiz_id, v_tag_id, p_created_by
        WHERE NOT EXISTS (
            SELECT 1 FROM "QuizTagMapping"
            WHERE quiz_id = v_quiz_id AND tag_id = v_tag_id
        );
    END LOOP;

    -- 7. Handle Questions
    FOR v_q IN
        SELECT * FROM jsonb_to_recordset(p_questions)
        AS q(id INT, "categoryId" INT, "queDifficultyId" INT, "queText" TEXT, "queTypeId" INT, "queOptionsAns" JSONB)
    LOOP
        IF v_q.id IS NULL THEN
            INSERT INTO "BaseQuestions" (category_id, que_difficulty_id, que_text, que_type_id, created_by)
            VALUES (v_q."categoryId", v_q."queDifficultyId", v_q."queText", v_q."queTypeId", p_created_by)
            RETURNING id INTO v_question_id;

            FOR v_opt IN
                SELECT * FROM jsonb_to_recordset(v_q."queOptionsAns")
                AS o(id INT, "questionId" INT, key TEXT, value TEXT)
            LOOP
                INSERT INTO "QuestionOptionsAnswers" (question_id, key, value, created_by)
                VALUES (v_question_id, v_opt.key, v_opt.value, p_created_by);
            END LOOP;
        ELSE
            v_question_id := v_q.id;
        END IF;

        -- Add mapping if not exists
        INSERT INTO "QuizToBaseQuestionMap" (quiz_id, que_id,created_by)
        SELECT v_quiz_id, v_question_id, p_created_by
        WHERE NOT EXISTS (
            SELECT 1 FROM "QuizToBaseQuestionMap"
            WHERE quiz_id = v_quiz_id AND que_id = v_question_id
        );
    END LOOP;

    -- 8. Return success message
    RETURN QUERY
    SELECT TRUE,
           CASE WHEN p_quiz_id IS NULL THEN 'Quiz created successfully.'
                ELSE 'Quiz updated successfully.'
           END;
END;
$$;
