-- =============================================
-- Author:       Bhumi Shah (updated)
-- Update Date:  2025-09-22
-- Description:  Returns navbar data for a user based on their role.
--               For players returns ProgressPercentage calculated as:
--                 (total_xp - current_level_min_exp) /
--                 (next_level_min_exp - current_level_min_exp) * 100
--               For admins returns NULL ProgressPercentage.
-- Usage:        SELECT * FROM get_user_navbar_data(p_user_id := 7, p_admin_role_id := 1);
-- =============================================

CREATE OR REPLACE FUNCTION get_user_navbar_data(
    p_user_id INT,
    p_admin_role_id INT 
)
RETURNS TABLE (
    "CurrentUserXp" INT,
    "ProgressPercentage" NUMERIC,
    "ProfilePic" VARCHAR,
    "NotificationCount" INT
) AS
$$
DECLARE
    v_user_role_id INT;
    v_total_xp INT;
    v_current_level INT;
    v_level_min_exp INT;
    v_next_level_min_exp INT;
    v_progress NUMERIC(5,2);
BEGIN
    -- Get role id of the user
    SELECT role_id INTO v_user_role_id
    FROM "Users"
    WHERE id = p_user_id;

    IF v_user_role_id = p_admin_role_id THEN
        -- Admin flow: progress NULL
        RETURN QUERY
        SELECT
            NULL::INT AS "CurrentUserXp",
            NULL::NUMERIC AS "ProgressPercentage",
            u.profile_pic::VARCHAR AS "ProfilePic",
            (
                SELECT COUNT(*)
                FROM "UserNotifications" un
                JOIN "Notifications" gn ON un.global_notification_id = gn.id
                WHERE un.is_read = FALSE
                  AND un.is_deleted = FALSE
                  AND gn.is_deleted = FALSE
            )::INT AS "NotificationCount"
        FROM "Users" u
        WHERE u.id = p_user_id;

    ELSE
        -- Player flow: get user's xp & level
        SELECT upd.total_xp, upd.current_level
        INTO v_total_xp, v_current_level
        FROM "UserPerformanceDetails" upd
        WHERE upd.user_id = p_user_id;

        IF NOT FOUND THEN
            -- If user has no performance row, return NULL progress but still profile & counts
            RETURN QUERY
            SELECT
                NULL::INT AS "CurrentUserXp",
                NULL::NUMERIC AS "ProgressPercentage",
                u.profile_pic::VARCHAR AS "ProfilePic",
                (
                    SELECT COUNT(*)
                    FROM "UserNotifications" un
                    JOIN "Notifications" gn ON un.global_notification_id = gn.id
                    WHERE un.is_read = FALSE
                      AND un.is_deleted = FALSE
                      AND gn.is_deleted = FALSE
                )::INT AS "NotificationCount"
            FROM "Users" u
            WHERE u.id = p_user_id;
            
            RETURN; -- ADD THIS LINE TO EXIT THE FUNCTION
        END IF;

        -- Get current level minimum_exp
        SELECT lpe.minimum_exp
        INTO v_level_min_exp
        FROM "LevelByExp" lpe
        WHERE lpe.level_order = v_current_level
        LIMIT 1;

        -- Get next level minimum_exp (level_order + 1)
        SELECT lpe2.minimum_exp
        INTO v_next_level_min_exp
        FROM "LevelByExp" lpe2
        WHERE lpe2.level_order = v_current_level + 1
        LIMIT 1;

        -- If no next level or denominator 0, user is at max level -> 100%
        IF v_next_level_min_exp IS NULL OR (v_next_level_min_exp - v_level_min_exp) = 0 THEN
            v_progress := 100.00;
        ELSE
            v_progress := ROUND(
                GREATEST(
                  LEAST(
                    (v_total_xp - v_level_min_exp)::NUMERIC
                    / (v_next_level_min_exp - v_level_min_exp) * 100
                  , 100)
                , 0)
            , 2);
        END IF;

        -- Return computed progress with profile and notification count
        RETURN QUERY
        SELECT
            v_total_xp AS "CurrentUserXp",
            v_progress AS "ProgressPercentage",
            u.profile_pic::VARCHAR AS "ProfilePic",
            (
                SELECT COUNT(*)
                FROM "UserNotifications" un
                JOIN "Notifications" gn ON un.global_notification_id = gn.id
                WHERE un.is_read = FALSE
                  AND un.is_deleted = FALSE
                  AND gn.is_deleted = FALSE
            )::INT AS "NotificationCount"
        FROM "Users" u
        WHERE u.id = p_user_id;
    END IF;
END;
$$ LANGUAGE plpgsql;