-- ==============================================================
-- Author:       <Vivek Kotadiya>
-- Create date:  <11-August-2025>
-- Description:  <Create or update a quiz category with logic to 
--               handle reactivation of soft-deleted categories, 
--               prevent duplicates, and ensure quiz/category 
--               integrity>
-- Usage:        SELECT * FROM create_or_update_quiz_category(
--                   p_id, p_category_name, p_description, 
--                   p_icon, p_user_id
--               );
-- ================================================================

CREATE OR REPLACE FUNCTION public.create_or_update_quiz_category(
	p_id integer,
	p_category_name character varying,
	p_description character varying,
	p_icon character varying,
	p_user_id integer)
    RETURNS TABLE(p_success boolean, p_message text) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
DECLARE
    v_existing_id INT;
    v_soft_deleted_id INT;
    v_quiz_exists BOOLEAN;
    v_base_question_exists BOOLEAN;
BEGIN
    -- Sanitize category name
    p_category_name := TRIM(p_category_name);

    -- UPDATE CASE
    IF p_id IS NOT NULL AND p_id > 0 THEN
        
        -- Check if category exists and is active
        SELECT id 
        INTO v_existing_id
        FROM "QuizCategory"
        WHERE id = p_id AND is_deleted = FALSE;

        IF v_existing_id IS NULL THEN
            RETURN QUERY SELECT FALSE, 'Quiz category not found or has been deleted.';
            RETURN;
        END IF;

        -- Check linked quizzes and base questions
        SELECT EXISTS (SELECT 1 FROM "Quiz" WHERE category_id = p_id)
        INTO v_quiz_exists;

        SELECT EXISTS (SELECT 1 FROM "BaseQuestions" WHERE category_id = p_id)
        INTO v_base_question_exists;

        -- Prevent name change if linked items exist
        IF (v_quiz_exists OR v_base_question_exists) AND LOWER(p_category_name) <> LOWER(
            (SELECT category_name FROM "QuizCategory" WHERE id = p_id)
        ) THEN
            RETURN QUERY SELECT FALSE, 'Quiz category name cannot be changed because it is linked to quizzes or questions.';
            RETURN;
        END IF;

        -- Check if same name exists in a soft-deleted category
        SELECT id 
        INTO v_soft_deleted_id
        FROM "QuizCategory"
        WHERE id <> p_id 
        AND LOWER(category_name) = LOWER(p_category_name) 
        AND is_deleted = TRUE;

        IF v_soft_deleted_id IS NOT NULL THEN
            -- Check if soft-deleted has links
            SELECT EXISTS (SELECT 1 FROM "Quiz" WHERE category_id = v_soft_deleted_id)
            INTO v_quiz_exists;

            SELECT EXISTS (SELECT 1 FROM "BaseQuestions" WHERE category_id = v_soft_deleted_id)
            INTO v_base_question_exists;

            IF v_quiz_exists OR v_base_question_exists THEN
                RETURN QUERY SELECT FALSE, 'Cannot update. A deleted quiz category with the same name has linked quizzes or questions.';
                RETURN;
            ELSE
                -- Safe to swap: deactivate current and reactivate old one
                UPDATE "QuizCategory"
                SET is_deleted = TRUE,
                    modified_date = CURRENT_TIMESTAMP,
                    modified_by = p_user_id
                WHERE id = p_id;

                UPDATE "QuizCategory"
                SET is_deleted = FALSE,
                    description = p_description,
                    icon = p_icon,
                    modified_date = CURRENT_TIMESTAMP,
                    modified_by = p_user_id
                WHERE id = v_soft_deleted_id;

                RETURN QUERY SELECT TRUE, 'Quiz category updated successfully.';
                RETURN;
            END IF;
        END IF;

        -- Prevent duplicate active category names
        IF EXISTS (
            SELECT 1 FROM "QuizCategory"
            WHERE id <> p_id 
            AND LOWER(category_name) = LOWER(p_category_name) 
            AND is_deleted = FALSE
        ) THEN
            RETURN QUERY SELECT FALSE, 'Quiz category with this name already exists.';
            RETURN;
        END IF;

        -- Perform normal update
        UPDATE "QuizCategory"
        SET category_name = p_category_name,
            description = p_description,
            icon = p_icon,
            modified_date = CURRENT_TIMESTAMP,
            modified_by = p_user_id
        WHERE id = p_id;

        RETURN QUERY SELECT TRUE, 'Quiz category updated successfully.';
        RETURN;

    -- CREATE CASE
    ELSE
        -- Check if soft-deleted category with same name exists
        SELECT id 
        INTO v_soft_deleted_id
        FROM "QuizCategory"
        WHERE LOWER(category_name) = LOWER(p_category_name)
        AND is_deleted = TRUE;

        IF v_soft_deleted_id IS NOT NULL THEN
            -- Reactivate soft-deleted category
            UPDATE "QuizCategory"
            SET is_deleted = FALSE,
                category_name = p_category_name,
                description = p_description,
                icon = p_icon,
                modified_date = CURRENT_TIMESTAMP,
                modified_by = p_user_id
            WHERE id = v_soft_deleted_id;

            RETURN QUERY SELECT TRUE, 'Quiz category created successfully.';
            RETURN;
        END IF;

        -- Prevent duplicate active category
        IF EXISTS (
            SELECT 1 FROM "QuizCategory"
            WHERE LOWER(category_name) = LOWER(p_category_name)
            AND is_deleted = FALSE
        ) THEN
            RETURN QUERY SELECT FALSE, 'Quiz category name already exists.';
            RETURN;
        END IF;

        -- Insert new category
        INSERT INTO "QuizCategory" (
            category_name, description, icon,
            created_date, created_by, is_deleted
        )
        VALUES (
            p_category_name, p_description, p_icon,    
            CURRENT_TIMESTAMP, p_user_id, FALSE
        );

        RETURN QUERY SELECT TRUE, 'Quiz category created successfully.';
        RETURN;
    END IF;

END;
$BODY$;
