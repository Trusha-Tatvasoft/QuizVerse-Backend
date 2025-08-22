-- ==============================================================
-- Author:       <Vivek Kotadiya>
-- Create date:  <22-Aug-2025>
-- Description:  <Get global leaderboard rankings for top 50 users 
--                plus the requested user. Includes rank, XP, 
--                level, streak, trend (1 = same, 2 = moved up, 
--                3 = moved down), and am_i flag.>
-- Example:      SELECT * FROM get_leaderboard_global_rankings(5);
-- ===============================================================

CREATE OR REPLACE FUNCTION public.get_leaderboard_global_rankings(
    p_user_id integer DEFAULT NULL
)
RETURNS TABLE (
    rank            integer,
    user_id         integer,
    user_name       varchar,
    full_name       varchar,
    profile_pic     varchar,
    total_xp        integer,
    current_level   integer,
    current_streak  integer,
    new_global_rank integer,
    trend           integer,  -- 1 = same, 2 = moved up, 3 = moved down
    am_i            boolean   -- Flag: true if this row belongs to current user
) 
LANGUAGE sql
COST 100
VOLATILE PARALLEL UNSAFE
ROWS 1000
AS $BODY$
-- Step 1: Prepare base leaderboard data
WITH base AS (
    SELECT 
        up.user_id,
        u.user_name,
        u.full_name,
        u.profile_pic,
        up.total_xp,
        up.current_level,
        up.current_streak,
        up.new_global_rank,
        up.old_global_rank,
        up.new_global_rank AS row_rank
    FROM "UserPerformanceDetails" up
    JOIN "Users" u ON u.id = up.user_id
    WHERE u.is_deleted = false
),

-- Step 2: Select top 50 users + always include current user (if provided)
final AS (
    SELECT * FROM base WHERE row_rank <= 50
    UNION
    SELECT * FROM base WHERE user_id = p_user_id
)

-- Step 3: Return leaderboard with trend calculation + am_i flag
SELECT 
    row_rank AS rank,
    user_id,
    user_name,
    full_name,
    profile_pic,
    total_xp,
    current_level,
    current_streak,
    new_global_rank,
    CASE 
        WHEN old_global_rank IS NULL OR new_global_rank IS NULL THEN 1  -- default = same
        WHEN new_global_rank < old_global_rank THEN 2                   -- rank improved
        WHEN new_global_rank > old_global_rank THEN 3                   -- rank dropped
        ELSE 1                                                          -- same
    END AS trend,
    (user_id = p_user_id) AS am_i
FROM final
ORDER BY row_rank;
$BODY$;