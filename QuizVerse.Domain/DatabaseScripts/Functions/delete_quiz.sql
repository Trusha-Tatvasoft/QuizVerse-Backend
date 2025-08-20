-- =============================================
-- Author:       Darsh Aswani
-- Create Date:  12-August-2025
-- Description:  Soft deletes a quiz by its ID after validating business rules:
--                 • Verifies quiz exists and is not already deleted
--                 • If quiz is paid, checks for purchases where the quiz
--                   has not yet been attempted by the purchasing user
--                 • Prevents deletion if there are unattempted purchases
--                 • Updates `is_deleted` flag, sets `modified_by` and `modified_date`
-- Parameters:    p_quiz_id     INT   - ID of the quiz to delete
--                 p_modified_by INT   - User ID performing the deletion
-- Returns:       TABLE (
--                   p_success BOOLEAN - TRUE if quiz deleted, FALSE otherwise
--                   p_message TEXT    - Success or error message
--                 )
-- Usage:         SELECT * FROM delete_quiz(18, 1);
-- =============================================

CREATE OR REPLACE FUNCTION delete_quiz(p_quiz_id INT, p_modified_by INT)
RETURNS TABLE (
    p_success BOOLEAN,
    p_message TEXT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_is_paid BOOLEAN;
    v_unattempted_purchases INT;
BEGIN 
    -- First check if quiz exists
    IF NOT EXISTS (
        SELECT 1
        FROM "Quiz"
        WHERE id = p_quiz_id
        AND is_deleted = FALSE
    ) THEN
        RETURN QUERY SELECT FALSE, 'Quiz not found.';
        RETURN;
    END IF;

    -- If exists, fetch is_paid
    SELECT is_paid
    INTO v_is_paid
    FROM "Quiz"
    WHERE id = p_quiz_id;

    -- If quiz is paid, check for unattempted purchases
    IF v_is_paid THEN
        -- Count users who purchased but didn't attempt
        SELECT COUNT(*) INTO v_unattempted_purchases
        FROM "QuizPurchased" qp
        WHERE qp.quiz_id = p_quiz_id
        AND NOT EXISTS (
            SELECT 1
            FROM "QuizAttempted" qa
            WHERE qa.quiz_id = p_quiz_id
            AND qa.user_id = qp.user_id
        );

        IF v_unattempted_purchases > 0 THEN
            RETURN QUERY SELECT FALSE, 
                'Quiz has been purchased by ' || v_unattempted_purchases || 
                ' user(s) who have not attempted it yet. Cannot delete.';
            RETURN;
        END IF;
    END IF;

    -- Soft delete the quiz
    UPDATE "Quiz"
    SET is_deleted = TRUE, modified_by = p_modified_by, modified_date = NOW()
    WHERE id = p_quiz_id;

    RETURN QUERY SELECT TRUE, 'Quiz deleted successfully.';
END;
$$;