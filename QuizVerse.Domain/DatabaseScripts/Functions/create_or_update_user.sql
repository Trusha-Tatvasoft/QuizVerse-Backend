-- =========================================================================================
-- Author:       Devisha Gajjar
-- Create Date:  11-September-2025
-- Description:  Creates a new user or updates an existing user.
--               Handles uniqueness constraints on email and username, 
--               prevents email changes during update,
--               skips creation if email is suspended,
--               and ensures performance details are initialized for new users.
-- Usage:        
--     -- Create a new user
--     SELECT * FROM create_or_update_user(
--         NULL,                               -- p_id (NULL for new user)
--         'Alice Smith',                      -- p_full_name
--         'alice@gmail.com',                  -- p_email
--         'alicesmith',                       -- p_username
--         'hashedpass',                       -- p_password
--         'profile_pic.jpg',                  -- p_profile_pic binary
--         'A passionate gamer',               -- p_bio
--         2,                                  -- p_player_role_id
--         1,                                  -- p_status_active
--         2,                                  -- p_status_inactive
--         3,                                  -- p_status_suspended
--         1,                                  -- p_modified_by
--         TRUE                                -- p_first_time_login 
--     );
--
--     -- Create a new user with first_time_login = FALSE (registration)
--     SELECT * FROM create_or_update_user(
--         NULL,
--         'Bob Jones',
--         'bob@gmail.com',
--         'bobjones',
--         'hashedpass',
--         'profile_pic2.jpg',
--         'New user bio',
--         2,
--         1,
--         2,
--         3,
--         1,
--         FALSE                            
--     );
--
--     -- Update an existing user (only username, bio, and profile pic)
--     SELECT * FROM create_or_update_user(
--          66,                                -- p_id (existing user ID)
--         'Alice Smith',                      -- p_full_name
--         'alice@gmail.com',                  -- p_email (must match existing)
--         'alice_updated',                    -- p_username (new username)
--         NULL,                               -- p_password (ignored in update)
--         'new_pic.jpg',                      -- p_profile_pic (optional)
--         'Updated bio info',                 -- p_bio
--         2,                                  -- p_player_role_id (ignored)
--         1,                                  -- p_status_active
--         2,                                  -- p_status_inactive
--         3,                                  -- p_status_suspended
--         1                                   -- p_modified_by
--     );
-- =========================================================================================

CREATE OR REPLACE FUNCTION create_or_update_user(
    p_id INT,
	p_full_name TEXT,
    p_email TEXT,
    p_username TEXT,
    p_password TEXT,
    p_profile_pic TEXT,
    p_bio TEXT,
    p_player_role_id INT,
    p_status_active INT,
    p_status_inactive INT,
    p_status_suspended INT,
    p_modified_by INT DEFAULT NULL,
    p_first_time_login BOOLEAN DEFAULT TRUE
)
RETURNS TABLE (
    p_success BOOLEAN,
    p_message TEXT
)
AS $$
DECLARE
    v_user RECORD;
    v_existing_user RECORD;
    v_new_user_id INT;
    v_trimmed_email TEXT;
    v_trimmed_full_name TEXT;
    v_trimmed_username TEXT;
    v_trimmed_bio TEXT;
BEGIN
    v_trimmed_email := TRIM(p_email);
    v_trimmed_full_name := TRIM(p_full_name);
    v_trimmed_username := TRIM(p_username);
    v_trimmed_bio := CASE WHEN p_bio IS NOT NULL THEN TRIM(p_bio) ELSE NULL END;

    IF p_id IS NOT NULL AND p_id > 0 THEN
        -- UPDATE 
        SELECT * INTO v_user FROM "Users" WHERE id = p_id AND NOT is_deleted;
        IF NOT FOUND THEN
            RETURN QUERY SELECT FALSE, format('User with ID %s not found.', p_id);
            RETURN;
        END IF;

        IF LOWER(v_user.email) <> LOWER(v_trimmed_email) THEN
            RETURN QUERY SELECT FALSE, 'Email can''t be changed';
            RETURN;
        END IF;

        IF EXISTS(SELECT 1 FROM "Users" WHERE user_name = v_trimmed_username AND id <> p_id) THEN
            RETURN QUERY SELECT FALSE, 'User with this username already exists.';
            RETURN;
        END IF;

        UPDATE "Users"
        SET user_name = v_trimmed_username,
            bio = v_trimmed_bio,
            profile_pic = COALESCE(p_profile_pic, v_user.profile_pic),
            full_name = v_trimmed_full_name,              
            modified_by = p_modified_by,
            modified_date = NOW()
        WHERE id = p_id;

        RETURN QUERY SELECT TRUE, 'User updated successfully.';
        RETURN;

    ELSE
        -- CREATE 
        SELECT * INTO v_existing_user FROM "Users" WHERE LOWER(email) = LOWER(v_trimmed_email);

        IF FOUND THEN
            IF v_existing_user.is_deleted AND
               v_existing_user.status IN (p_status_active, p_status_inactive) THEN
               PERFORM cleanup_old_user(v_existing_user.id, p_modified_by);
            ELSIF v_existing_user.status = p_status_suspended THEN
                RETURN QUERY SELECT FALSE, 'This account has been suspended. Please contact support.';
                RETURN;
            ELSE
                RETURN QUERY SELECT FALSE, 'User with this email already exists.';
                RETURN;
            END IF;
        END IF;

        IF EXISTS(SELECT 1 FROM "Users" WHERE user_name = v_trimmed_username) THEN
            RETURN QUERY SELECT FALSE, 'User with this username already exists.';
            RETURN;
        END IF;

        -- Insert into Users
        INSERT INTO "Users"(full_name,
            email, user_name, password, bio, profile_pic, role_id, status,
            created_date, created_by, first_time_login, is_deleted
        )
        VALUES (
            v_trimmed_full_name,
            v_trimmed_email,
            v_trimmed_username,
            p_password,
            v_trimmed_bio,
            p_profile_pic,              
            p_player_role_id,
            p_status_active,
            NOW(),
            p_modified_by,
            p_first_time_login,
            FALSE
        )
        RETURNING id INTO v_new_user_id;

        -- Insert into UserPerformanceDetails
        INSERT INTO "UserPerformanceDetails" (
            user_id,
            total_xp,
            old_global_rank,
            new_global_rank,
            current_level,
            current_streak,
            highest_streak,
            created_date,
            modified_date
        )
        VALUES (
            v_new_user_id,
            0,
            0,
            0,
            1,
            0,
            0,
            NOW(),
            NOW()
        );

        RETURN QUERY SELECT TRUE, 'User created successfully.';
        RETURN;
    END IF;
END;
$$ LANGUAGE plpgsql;
