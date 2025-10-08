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

    -- Rank and XP details
    current_rank_min_level INT;
    current_rank_max_level INT;
    next_rank_min_level INT;

    current_rank_min_exp INT;
    current_rank_max_exp INT;
    next_rank_min_exp INT;

    current_rank_name TEXT;
    next_rank_name TEXT;
BEGIN
    -- 1. Get user's XP and level
    SELECT total_xp, current_level
    INTO user_total_xp, user_current_level
    FROM "UserPerformanceDetails"
    WHERE user_id = p_user_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'User not found';
    END IF;

    -- 2. Find user's current rank range
    SELECT rank_name, minimum_level, maximum_level
    INTO current_rank_name, current_rank_min_level, current_rank_max_level
    FROM "UserRankByLevel"
    WHERE minimum_level <= user_current_level
      AND maximum_level >= user_current_level
    LIMIT 1;

    -- 3. Find next rank range
    SELECT rank_name, minimum_level
    INTO next_rank_name, next_rank_min_level
    FROM "UserRankByLevel"
    WHERE minimum_level > user_current_level
    ORDER BY minimum_level
    LIMIT 1;

    -- 4. Get XP range for current rank (from all levels in LevelByExp)
    SELECT MIN(minimum_exp), MAX(maximum_exp)
    INTO current_rank_min_exp, current_rank_max_exp
    FROM "LevelByExp"
    WHERE level_order BETWEEN current_rank_min_level AND current_rank_max_level;

    -- 5. Get XP start of next rank
    SELECT MIN(minimum_exp)
    INTO next_rank_min_exp
    FROM "LevelByExp"
    WHERE level_order >= next_rank_min_level;

    -- 6. Calculate XP needed and progress percent
    IF next_rank_min_exp IS NULL THEN
        -- User is at max rank
        current_rank := current_rank_name;
        next_rank := NULL;
        xp_needed := 0;
        progress_percent := 100;
    ELSE
        current_rank := current_rank_name;
        next_rank := next_rank_name;
        xp_needed := next_rank_min_exp - user_total_xp;
        progress_percent := ROUND(
            (user_total_xp - current_rank_min_exp)::NUMERIC /
            (next_rank_min_exp - current_rank_min_exp) * 100, 2
        );
    END IF;

    RETURN NEXT;
END;
$$;