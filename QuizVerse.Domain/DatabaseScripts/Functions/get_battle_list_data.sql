-- =============================================
-- Author:       Bhumi Shah
-- Create Date:  14-August-2025
-- Description:  Returns the list of battles with details including:
--                 • Battle ID, start date, end date
--                 • Battle type (Permanent = p_permanent, Time Limited = p_time_limited)
--                 • Battle name, description, category, and difficulty
--                 • Total XP and total questions for each battle
--                 • Total unique participants (active users only, excluding running battles)
--                 • Current battle status (auto-updated based on end_date):
--                       - If end_date < today → marked as Completed (p_completed_battle_status)
--                       - If end_date >= today → marked as Active (p_active_battle_status)
--               Joins BattleList → Quiz → Category → Difficulty and 
--               counts distinct participants from BattleStatus (ignores deleted users and running battles).
--               Only includes non-deleted battles, quizzes, categories, and users.
-- Usage:        SELECT * 
--               FROM get_battle_list_data(
--                    p_permanent := 1, 
--                    p_time_limited := 2, 
--                    p_active_battle_status := 3, 
--                    p_completed_battle_status := 4, 
--                    p_running := 5
--               );
-- =============================================

CREATE OR REPLACE FUNCTION get_battle_list_data(
    p_permanent INT,         
    p_time_limited INT,
    p_active_battle_status INT,
    p_completed_battle_status INT,    
    p_running INT            
)
RETURNS TABLE (
    Id INT,
    StartDate TIMESTAMP,
    EndDate TIMESTAMP,
    BattleTime INT,
    BattleName VARCHAR,
    Description VARCHAR,
    CategoryName VARCHAR,
    BattleDifficulty VARCHAR,
    TotalXp INT,
    TotalParticipants INT,
    TotalQuestion INT,
    BattleStatus INT
) AS $$
BEGIN
    -- Mark battles as completed if end_date (date only) has already passed
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

    -- Mark battles as active if end_date (date only) is today or later
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
    SELECT
        b.id,
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
    ORDER BY b.id;
END;
$$ LANGUAGE plpgsql;