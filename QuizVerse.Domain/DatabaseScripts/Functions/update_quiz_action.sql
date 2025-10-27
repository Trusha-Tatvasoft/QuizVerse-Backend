-- =============================================
-- Author:       Darsh Aswani
-- Create Date:  12-August-2025
-- Updated Date: 17-October-2025
-- Description:  Performs quiz delete / activate / inactivate operations.
-- =============================================

CREATE OR REPLACE FUNCTION update_quiz_action(
    p_quiz_id INT,
    p_is_deleted_action BOOLEAN,
    p_is_active_status BOOLEAN,
    p_modified_by INT
)
RETURNS TABLE (
    p_success BOOLEAN,
    p_message TEXT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_is_paid BOOLEAN;
    v_unattempted_purchases INT;
    v_action TEXT;
BEGIN
    -- Check if quiz exists and not already deleted
    IF NOT EXISTS (
        SELECT 1
        FROM "Quiz"
        WHERE id = p_quiz_id
        AND is_deleted = FALSE
    ) THEN
        RETURN QUERY SELECT FALSE, 'Quiz not found or already deleted.';
        RETURN;
    END IF;

    -- ======================================================
    -- CASE 1: DELETE or INACTIVATE (same validation logic)
    -- ======================================================
    IF p_is_deleted_action OR p_is_active_status = FALSE THEN
        v_action := CASE 
            WHEN p_is_deleted_action THEN 'delete'
            ELSE 'inactivate'
        END;

        IF EXISTS (
            SELECT 1 
            FROM "QuizPlayStatus"
            WHERE quiz_id = p_quiz_id AND is_completed = FALSE
        ) THEN
            RETURN QUERY SELECT FALSE, 
                'Someone is currently playing this quiz. Cannot ' || v_action || '.';
            RETURN;
        END IF;

        --Check if paid
        SELECT is_paid INTO v_is_paid FROM "Quiz" WHERE id = p_quiz_id;

        IF v_is_paid THEN
            SELECT COUNT(*) INTO v_unattempted_purchases
            FROM "QuizPurchased" qp
            WHERE qp.quiz_id = p_quiz_id
              AND NOT EXISTS (
                SELECT 1 FROM "QuizAttempted" qa
                WHERE qa.quiz_id = p_quiz_id AND qa.user_id = qp.user_id
              );

            IF v_unattempted_purchases > 0 THEN
                RETURN QUERY SELECT FALSE,
                    'Quiz has been purchased by ' || v_unattempted_purchases ||
                    ' user(s) who have not attempted it yet. Cannot ' || v_action || '.';
                RETURN;
            END IF;
        END IF;

        -- Perform action (delete or inactivate)
        IF p_is_deleted_action THEN
            UPDATE "Quiz"
            SET is_deleted = TRUE, modified_by = p_modified_by, modified_date = NOW()
            WHERE id = p_quiz_id;
            RETURN QUERY SELECT TRUE, 'Quiz deleted successfully.';
        ELSE
            UPDATE "Quiz"
            SET status = 3, modified_by = p_modified_by, modified_date = NOW()
            WHERE id = p_quiz_id;
            RETURN QUERY SELECT TRUE, 'Quiz inactivated successfully.';
        END IF;
        RETURN;
    END IF;

    -- ======================================================
    -- CASE 2: ACTIVATE
    -- ======================================================
    IF p_is_active_status = TRUE THEN
        UPDATE "Quiz"
        SET status = 1, modified_by = p_modified_by, modified_date = NOW()
        WHERE id = p_quiz_id;

        RETURN QUERY SELECT TRUE, 'Quiz activated successfully.';
        RETURN;
    END IF;

    -- ======================================================
    -- CASE 3: INVALID PARAMETERS
    -- ======================================================
    RETURN QUERY SELECT FALSE, 'Invalid action parameters provided.';
END;
$$;
