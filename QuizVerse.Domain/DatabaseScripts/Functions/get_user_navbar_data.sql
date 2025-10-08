-- =============================================
-- Author:       Bhumi Shah
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

    v_rank_name TEXT;
    v_rank_min_level INT;
    v_rank_max_level INT;

    v_next_rank_name TEXT;
    v_next_rank_min_level INT;

    v_rank_min_exp BIGINT;
    v_next_rank_min_exp BIGINT;

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

        -- Get current rank info by level (if any)
        SELECT r.rank_name, r.minimum_level, r.maximum_level
        INTO v_rank_name, v_rank_min_level, v_rank_max_level
        FROM "UserRankByLevel" r
        WHERE v_current_level BETWEEN r.minimum_level AND r.maximum_level
        LIMIT 1;

        -- Get next rank's minimum level (the next rank after current rank)
        SELECT r.rank_name, r.minimum_level
        INTO v_next_rank_name, v_next_rank_min_level
        FROM "UserRankByLevel" r
        WHERE r.minimum_level > COALESCE(v_rank_min_level, v_current_level)
        ORDER BY r.minimum_level ASC
        LIMIT 1;

        -- current rank min XP (take nearest LevelByExp <= min_level)
        SELECT minimum_exp
        INTO v_rank_min_exp
        FROM "LevelByExp"
        WHERE level_order <= v_rank_min_level
        ORDER BY level_order DESC
        LIMIT 1;

        -- next rank min XP (if next rank exists)
        IF v_next_rank_min_level IS NOT NULL THEN
            SELECT minimum_exp
            INTO v_next_rank_min_exp
            FROM "LevelByExp"
            WHERE level_order <= v_next_rank_min_level
            ORDER BY level_order DESC
            LIMIT 1;
        ELSE
            v_next_rank_min_exp := NULL;
        END IF;

        -- Treat missing lower boundary as 0 XP
        IF v_rank_min_exp IS NULL THEN
            v_rank_min_exp := 0;
        END IF;

        -- If no next rank boundary (top rank) or zero denominator => 100%
        IF v_next_rank_min_exp IS NULL OR (v_next_rank_min_exp - v_rank_min_exp) = 0 THEN
            v_progress := 100.00;
        ELSE
            v_progress := ROUND(
                GREATEST(
                  LEAST(
                    (v_total_xp - v_rank_min_exp)::NUMERIC
                    / (v_next_rank_min_exp - v_rank_min_exp) * 100
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