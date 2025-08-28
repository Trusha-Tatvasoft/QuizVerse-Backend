-- =============================================
-- Author:      <Zeel Vaghasiya>
-- Create date: <25-August-2025>
-- Description: <Get user rank progress including current rank,
--               next rank, XP needed, and progress percent>
-- Usage:       SELECT * FROM get_rank_progress(p_user_id);
-- =============================================

CREATE OR REPLACE FUNCTION get_rank_progress(p_user_id INT)
RETURNS TABLE(
    current_rank TEXT,
    next_rank TEXT,
    xp_needed INT,
    progress_percent NUMERIC
)
LANGUAGE plpgsql
AS $$
DECLARE
    user_total_xp INT;
    user_current_level INT;

    current_level_min_exp INT;
    current_level_max_exp INT;

    next_level_min_exp INT;

    current_rank_name TEXT;
    next_rank_name TEXT;
BEGIN
    -- 1. Get user performance
    SELECT total_xp, current_level
    INTO user_total_xp, user_current_level
    FROM "UserPerformanceDetails"
    WHERE user_id = p_user_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'User not found';
    END IF;

    -- 2. Get current level by XP
    SELECT minimum_exp, maximum_exp
    INTO current_level_min_exp, current_level_max_exp
    FROM "LevelByExp"
    WHERE minimum_exp <= user_total_xp
      AND maximum_exp >= user_total_xp
    LIMIT 1;

    -- 3. Get next level
    SELECT minimum_exp
    INTO next_level_min_exp
    FROM "LevelByExp"
    WHERE minimum_exp > user_total_xp
    ORDER BY level_order
    LIMIT 1;

    -- 4. Get current rank
    SELECT rank_name
    INTO current_rank_name
    FROM "UserRankByLevel"
    WHERE minimum_level <= user_current_level
      AND maximum_level >= user_current_level
    LIMIT 1;

    -- 5. Get next rank
    SELECT rank_name
    INTO next_rank_name
    FROM "UserRankByLevel"
    WHERE minimum_level > user_current_level
    ORDER BY minimum_level
    LIMIT 1;

    -- 6. Calculate XP needed and progress percent
    IF next_level_min_exp IS NULL THEN
        -- Max level reached
        current_rank := current_rank_name;
        next_rank := NULL;
        xp_needed := 0;
        progress_percent := 100;
    ELSE
        current_rank := current_rank_name;
        next_rank := next_rank_name;
        xp_needed := next_level_min_exp - user_total_xp;
        progress_percent := ROUND(
            (user_total_xp - current_level_min_exp)::NUMERIC /
            (next_level_min_exp - current_level_min_exp) * 100, 2
        );
    END IF;

    RETURN NEXT;
END;
$$;