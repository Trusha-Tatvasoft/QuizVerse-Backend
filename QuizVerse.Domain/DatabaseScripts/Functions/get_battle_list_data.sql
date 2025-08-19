-- =============================================
-- Author:       Bhumi Shah
-- Create Date:  14-August-2025
-- Description:  Returns the list of battles with details including:
--                 • Battle ID, start date, end date
--                 • Battle type (Permanent = 1, Time Limited = 2)
--                 • Battle name, description, category, difficulty
--                 • Total XP and total questions for the battle
--                 • Total unique participants (active users only) for completed battles
--                 • Current battle status
--               Joins BattleList → Quiz → Category → Difficulty and 
--               counts participants from BattleStatus.
--               Only includes non-deleted battles, quizzes, categories, and users.
-- Usage:        SELECT * FROM get_battle_list_data(p_permanent := 1, p_time_limited := 2, p_running := 3);
-- =============================================

CREATE OR REPLACE FUNCTION get_battle_list_data(
    p_permanent INT,         
    p_time_limited INT,      
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
	Order by b.id;
END;
$$ LANGUAGE plpgsql;