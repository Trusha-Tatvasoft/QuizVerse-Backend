-- ==============================================================================
-- Author:       <Devisha Gajjar>
-- Create date:  <26-August-2025>
-- Description:  <Fetches the basic profile of a user including rank, XP, progress,
--               achievements, and win rate with calculated statistics>
-- Usage:        SELECT * FROM get_user_basic_profile(p_user_id);
-- ==============================================================================

CREATE OR REPLACE FUNCTION get_user_basic_profile(p_user_id INT)
RETURNS TABLE (
    "UserId" INT,
    "ProfilePic" VARCHAR,
    "Name" VARCHAR,
    "Rank" VARCHAR,
    "NextRank" VARCHAR,
    "MemberSince" TIMESTAMPTZ,
    "Progress" NUMERIC,
    "TotalXp" INT,  
    "QuizCompleted" INT,
    "WinRate" NUMERIC,
    "Achievements" INT
) AS $$
DECLARE
    v_total_xp INT := 0;
    v_current_level INT := 1;
    v_rank_name VARCHAR := 'Unranked';
    v_next_rank_name VARCHAR := 'N/A';
 
    v_rank_min_level INT := 0;
    v_rank_max_level INT := 0;
    v_next_rank_min_level INT := NULL;
 
    v_rank_min_exp BIGINT := 0;
    v_next_rank_min_exp BIGINT := NULL;
 
    v_progress_pct NUMERIC(5,2) := 0;
BEGIN
    -- Get total XP and current level
    SELECT upd.total_xp, upd.current_level
    INTO v_total_xp, v_current_level
    FROM "UserPerformanceDetails" upd
    WHERE upd.user_id = p_user_id;
 
    IF NOT FOUND THEN
        RETURN QUERY
        SELECT
            u.id AS "UserId",
            u.profile_pic AS "ProfilePic",
            u.full_name AS "Name",
            'Unranked' AS "Rank",
            'N/A' AS "NextRank",
            u.created_date AS "MemberSince",
            0::NUMERIC AS "Progress",
            0 AS "TotalXp",
            0 AS "QuizCompleted",
            0 AS "WinRate",
            0 AS "Achievements"
        FROM "Users" u
        WHERE u.id = p_user_id;
        RETURN;
    END IF;
 
    -- Get current rank info by level
    SELECT r.rank_name, r.minimum_level, r.maximum_level
    INTO v_rank_name, v_rank_min_level, v_rank_max_level
    FROM "UserRankByLevel" r
    WHERE v_current_level BETWEEN r.minimum_level AND r.maximum_level
    LIMIT 1;
 
    -- Get next rank info
    SELECT r.rank_name, r.minimum_level
    INTO v_next_rank_name, v_next_rank_min_level
    FROM "UserRankByLevel" r
    WHERE r.minimum_level > COALESCE(v_rank_min_level, v_current_level)
    ORDER BY r.minimum_level ASC
    LIMIT 1;
 
    -- Get XP range for current rank
    SELECT minimum_exp
    INTO v_rank_min_exp
    FROM "LevelByExp"
    WHERE level_order <= v_rank_min_level
    ORDER BY level_order DESC
    LIMIT 1;
 
    IF v_next_rank_min_level IS NOT NULL THEN
        SELECT minimum_exp
        INTO v_next_rank_min_exp
        FROM "LevelByExp"
        WHERE level_order <= v_next_rank_min_level
        ORDER BY level_order DESC
        LIMIT 1;
    END IF;
 
    -- Default lower boundary if missing
    IF v_rank_min_exp IS NULL THEN
        v_rank_min_exp := 0;
    END IF;
 
    -- Calculate XP-based progress within rank boundaries
    IF v_next_rank_min_exp IS NULL OR (v_next_rank_min_exp - v_rank_min_exp) = 0 THEN
        v_progress_pct := 100;
    ELSE
        v_progress_pct := ROUND(
            GREATEST(
                LEAST(
                    (v_total_xp - v_rank_min_exp)::NUMERIC /
                    (v_next_rank_min_exp - v_rank_min_exp) * 100,
                100),
            0),
        2);
    END IF;
 
    -- Return the full user profile row
    RETURN QUERY
    SELECT
        u.id AS "UserId",
        u.profile_pic AS "ProfilePic",
        u.full_name AS "Name",
        COALESCE(v_rank_name, 'Unranked') AS "Rank",
        COALESCE(v_next_rank_name, 'N/A') AS "NextRank",
        u.created_date AS "MemberSince",
        COALESCE(v_progress_pct, 0) AS "Progress",
        v_total_xp AS "TotalXp",
        (
            SELECT COUNT(*)::INT
            FROM "QuizAttempted" qa
            WHERE qa.user_id = u.id
        ) AS "QuizCompleted",
        (
            SELECT COALESCE(
                ROUND(
                    100.0 * SUM(
                        CASE
                            WHEN br.winner_id = u.id THEN 1       -- user won
                            ELSE 0
                        END
                    )::NUMERIC / NULLIF(COUNT(*), 0),
                2),
            0)
            FROM "BattleResult" br
            JOIN "BattleStatus" bs ON bs.id = br.battle_status
            WHERE (bs.user1_id = u.id OR bs.user2_id = u.id)
              AND bs.battle_status IN (1, 2)
        ) AS "WinRate",
        (
            SELECT COUNT(*)::INT
            FROM "UserBadgesEarned" ub
            WHERE ub.user_id = u.id
        ) AS "Achievements"
    FROM "Users" u
    WHERE u.id = p_user_id;
END;
$$ LANGUAGE plpgsql;