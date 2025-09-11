-- ==============================================================
-- Author:       <Vivek Kotadiya>
-- Create date:  <22-Aug-2025>
-- Description:  <Get global leaderboard rankings for top 50 users 
--                plus the requested user. Includes rank, XP, 
--                level, streak, trend (1 = same, 2 = moved up, 
--                3 = moved down), and is_loggedin_user flag.>
-- Example:      SELECT * FROM get_leaderboard_global_rankings(5);
-- ===============================================================

CREATE OR REPLACE FUNCTION public.get_leaderboard_global_rankings(
    p_user_id integer DEFAULT NULL
)
RETURNS TABLE(
    rank integer,
    user_id integer,
    user_name character varying,
    full_name character varying,
    profile_pic character varying,
    total_xp integer,
    current_level integer,
    current_streak integer,
    trend integer,
    is_loggedin_user boolean
)
LANGUAGE sql
AS $BODY$
WITH ranked AS (
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
        DENSE_RANK() OVER (ORDER BY up.new_global_rank ASC) AS calculated_rank
    FROM "UserPerformanceDetails" up
    JOIN "Users" u ON u.id = up.user_id
    WHERE u.is_deleted = false
      AND up.new_global_rank > 0
)
SELECT 
    r.calculated_rank AS rank,
    r.user_id,
    r.user_name,
    r.full_name,
    r.profile_pic,
    r.total_xp,
    r.current_level,
    r.current_streak,
    CASE
        WHEN r.old_global_rank IS NULL OR r.new_global_rank IS NULL THEN 1
        WHEN r.new_global_rank < r.old_global_rank THEN 2
        WHEN r.new_global_rank > r.old_global_rank THEN 3
        ELSE 1
    END AS trend,
    (r.user_id = p_user_id) AS is_loggedin_user
FROM ranked r
ORDER BY r.calculated_rank;
$BODY$;