-- ==============================================================================
-- Author:       <Devisha Gajjar>
-- Create date:  <27-August-2025>
-- Description:  <Updates user profile settings (email, name, bio) while handling 
--               email conflicts by merging/cleaning up duplicate users>
-- Usage:        SELECT * FROM update_user_setting(p_current_user_id, p_new_email, p_new_name, p_new_bio);
-- ==============================================================================

CREATE OR REPLACE FUNCTION update_user_setting(
    p_current_user_id INT,
    p_new_email VARCHAR,
    p_new_name VARCHAR DEFAULT NULL,
    p_new_bio VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    p_success BOOLEAN,
    p_message TEXT
) AS $$
DECLARE
    v_old_user_id INT;
    v_exists BOOLEAN;
BEGIN
    -- Check if current user exists
    SELECT EXISTS (
        SELECT 1 FROM "Users" WHERE id = p_current_user_id
    ) INTO v_exists;

    IF NOT v_exists THEN
        RETURN QUERY SELECT FALSE, 'User does not exist';
        RETURN;
    END IF;

    -- 1. check if new email exists
    SELECT id INTO v_old_user_id
    FROM "Users"
    WHERE LOWER(email) = LOWER(TRIM(p_new_email))
    LIMIT 1;

    -- If no old user with that email -> simple update
    IF v_old_user_id IS NULL THEN
        UPDATE "Users"
        SET email = p_new_email,
            full_name = COALESCE(p_new_name, full_name),
            bio = COALESCE(p_new_bio, bio),
            modified_date = CURRENT_TIMESTAMP
        WHERE id = p_current_user_id;

        RETURN QUERY SELECT TRUE, 'Profile updated successfully';
        RETURN;
    END IF;

    -- If found same user (same id) → just update
    IF v_old_user_id = p_current_user_id THEN
        UPDATE "Users"
        SET email = p_new_email,
            full_name = COALESCE(p_new_name, full_name),
            bio = COALESCE(p_new_bio, bio),
            modified_date = CURRENT_TIMESTAMP
        WHERE id = p_current_user_id;

        RETURN QUERY SELECT TRUE, 'Profile updated successfully';
        RETURN;
    END IF;

    -- 2. Cleanup old user
    PERFORM cleanup_old_user(v_old_user_id);

    -- 3. Update current user
    UPDATE "Users"
    SET email = p_new_email,
        full_name = COALESCE(p_new_name, full_name),
        bio = COALESCE(p_new_bio, bio),
        modified_date = CURRENT_TIMESTAMP
    WHERE id = p_current_user_id;

    RETURN QUERY SELECT TRUE, 'Profile updated successfully';
END;
$$ LANGUAGE plpgsql;