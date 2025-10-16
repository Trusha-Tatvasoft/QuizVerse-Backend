-- =============================================
-- Author:       Bhumi Shah
-- Create Date:  14-August-2025
-- Description:  Returns a paginated list of battles with detailed metadata, including:
--                 • Battle ID, start and end dates
--                 • Battle type (Permanent = p_permanent, Time Limited = p_time_limited)
--                 • Battle name, description, category, and difficulty
--                 • Total XP, total questions, and total unique participants
--                   (participants are active users only, excluding running battles)
--                 • Current battle status, automatically updated as follows:
--                       - end_date < today  → marked as Completed (p_completed_battle_status)
--                       - end_date >= today → marked as Active (p_active_battle_status)
--
--               Supports pagination using:
--                 • p_batch_number → Current page index (default = 1)
--                 • Each batch returns up to 10 records
--                 • Also returns a "HasMore" flag to indicate if more data exists
--
--               Joins BattleList → Quiz → Category → Difficulty tables and
--               aggregates participant counts from BattleStatus (ignoring deleted users
--               and in-progress (running) battles).
--
--               Includes only non-deleted battles, quizzes, categories, and users.
--
-- Usage Example:
--     SELECT *
--     FROM get_battles_list_data(
--          p_permanent := 1,
--          p_time_limited := 2,
--          p_active_battle_status := 3,
--          p_completed_battle_status := 4,
--          p_running := 5,
--          p_batch_number := 1
--     );
-- =============================================

CREATE OR REPLACE FUNCTION get_battles_list_data(
    p_permanent INT,         
    p_time_limited INT,
    p_active_battle_status INT,
    p_completed_battle_status INT,    
    p_running INT,
    p_batch_number INT DEFAULT 1
)
RETURNS TABLE (
    "Battles" JSONB,
    "HasMore" BOOLEAN
) AS $$
DECLARE
    v_page_size INT := 10;
    v_offset INT := (p_batch_number - 1) * v_page_size;
BEGIN
    -- Update statuses (same logic you already have)
    UPDATE "Quiz" q
    SET status = p_completed_battle_status
    FROM "BattleList" b
    WHERE q.id = b.quiz_id
      AND b.is_deleted = FALSE
      AND q.is_deleted = FALSE
      AND b.start_date IS NOT NULL
      AND b.end_date IS NOT NULL
      AND b.end_date::date < NOW()::date
      AND q.status != p_completed_battle_status;

    UPDATE "Quiz" q
    SET status = p_active_battle_status
    FROM "BattleList" b
    WHERE q.id = b.quiz_id
      AND b.is_deleted = FALSE
      AND q.is_deleted = FALSE
      AND b.start_date IS NOT NULL
      AND b.end_date IS NOT NULL
      AND b.end_date::date >= NOW()::date
      AND q.status != p_active_battle_status;

    RETURN QUERY
    WITH all_battles AS (
        SELECT
            b.id AS "Id",
            b.start_date::TIMESTAMP AS "StartDate",
            b.end_date::TIMESTAMP AS "EndDate",
            CASE 
                WHEN b.battle_time_limited = TRUE THEN p_time_limited 
                ELSE p_permanent 
            END AS "BattleTime",
            q.name AS "BattleName",
            q.description AS "Description",
            c.category_name AS "CategoryName",
            d.name AS "BattleDifficulty",
            q.total_xp AS "TotalXp",
            (
                SELECT COUNT(DISTINCT uid)
                FROM (
                    SELECT bs.user1_id AS uid
                    FROM "BattleStatus" bs
                    JOIN "Users" u ON u.id = bs.user1_id
                    WHERE bs.battle_id = b.id
                      AND bs.battle_status != p_running
                      AND bs.is_deleted = FALSE
                      AND u.is_deleted = FALSE
                    UNION
                    SELECT bs.user2_id AS uid
                    FROM "BattleStatus" bs
                    JOIN "Users" u ON u.id = bs.user2_id
                    WHERE bs.battle_id = b.id
                      AND bs.battle_status != p_running
                      AND bs.is_deleted = FALSE
                      AND u.is_deleted = FALSE
                ) AS participants
            )::INT AS "TotalParticipants",
            q.total_question AS "TotalQuestion",
            q.status AS "BattleStatus"
        FROM "BattleList" b
        JOIN "Quiz" q ON q.id = b.quiz_id
        JOIN "QuizCategory" c ON c.id = q.category_id
        JOIN "QuizDifficulty" d ON d.id = q.difficulty_level_id
        WHERE b.is_deleted = FALSE
          AND q.is_deleted = FALSE
          AND c.is_deleted = FALSE
        ORDER BY b.id DESC
    ),
    total_count AS (
        SELECT COUNT(*) AS cnt FROM all_battles
    ),
    paged AS (
        SELECT *
        FROM all_battles
        OFFSET v_offset
        LIMIT v_page_size
    )
    SELECT
        COALESCE(
            (SELECT jsonb_agg(row) FROM (SELECT * FROM paged) AS row),
            '[]'::jsonb
        ) AS "Battles",
        (SELECT cnt > v_offset + v_page_size FROM total_count) AS "HasMore";
END;
$$ LANGUAGE plpgsql;
