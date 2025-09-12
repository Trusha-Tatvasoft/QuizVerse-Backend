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
    v_progress_pct NUMERIC := 0;
BEGIN
    -- Get total XP and current level
    SELECT upd.total_xp, upd.current_level
    INTO v_total_xp, v_current_level
    FROM "UserPerformanceDetails" upd
    WHERE upd.user_id = p_user_id;

    -- Get current rank name based on current level
    SELECT COALESCE(r.rank_name, 'Unranked')
    INTO v_rank_name
    FROM "UserRankByLevel" r
    WHERE v_current_level BETWEEN r.minimum_level AND r.maximum_level
    LIMIT 1;

    -- Get next rank name
    SELECT COALESCE(r.rank_name, 'N/A')
    INTO v_next_rank_name
    FROM "UserRankByLevel" r
    WHERE r.minimum_level > v_current_level
    ORDER BY r.minimum_level ASC
    LIMIT 1;

    -- Calculate progress percentage within current level
    SELECT ROUND(
        100.0 * (v_total_xp - COALESCE(l.minimum_exp, 0))::NUMERIC /
        NULLIF((COALESCE(l.maximum_exp, 0) - COALESCE(l.minimum_exp, 0)), 0), 2)
    INTO v_progress_pct
    FROM "LevelByExp" l
    WHERE v_total_xp BETWEEN l.minimum_exp AND l.maximum_exp
    LIMIT 1;

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