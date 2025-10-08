-- =============================================
-- Author:      <Zeel Vaghasiya>
-- Create date: <08-September-2025>
-- Description: <Get all available battles for a user including
--               battle details, category, difficulty, XP, total questions,
--               duration (in minutes), and participant count.
--               Excludes battles already played by the user.>
-- Usage:       SELECT * FROM get_user_available_battles(93);
-- =============================================
 
CREATE OR REPLACE FUNCTION get_user_available_battles(p_user_id INT)
RETURNS TABLE (
    "BattleId" INT,
    "BattleName" VARCHAR,
    "Category" VARCHAR,
    "Difficulty" VARCHAR,
    "Description" VARCHAR,
    "MaxXP" INT,
    "TotalQuestions" INT,
    "Duration" INTERVAL,
    "Participants" INT,
    "IsBattleRunning" BOOLEAN
) AS $$
BEGIN
    -- Mark battles as completed if end_date (date only) has already passed
    UPDATE "Quiz" q
    SET status = 2
    FROM "BattleList" b
    WHERE q.id = b.quiz_id
    AND b.is_deleted = FALSE
    AND q.is_deleted = FALSE
    AND b.start_date IS NOT NULL
    AND b.end_date IS NOT NULL
    AND b.end_date::date < NOW()::date
    AND q.status != 2;
 
    -- Mark battles as active if end_date (date only) is today or later
    UPDATE "Quiz" q
    SET status = 1
    FROM "BattleList" b
    WHERE q.id = b.quiz_id
    AND b.is_deleted = FALSE
    AND q.is_deleted = FALSE
    AND b.start_date IS NOT NULL
    AND b.end_date IS NOT NULL
    AND b.end_date::date >= NOW()::date
    AND q.status != 1;
 
    RETURN QUERY
    WITH active_battles AS (
        SELECT bl.id AS battle_id,
               q.name AS battle_name,
               qc.category_name,
               q.description,
               qd.name AS difficulty_name
        FROM "BattleList" bl
        JOIN "Quiz" q ON bl.quiz_id = q.id
        JOIN "QuizCategory" qc ON q.category_id = qc.id
        JOIN "QuizDifficulty" qd ON q.difficulty_level_id = qd.id
        WHERE bl.is_deleted = FALSE
          AND (
                -- Case 1: Not time-limited -> always available
                bl.battle_time_limited = FALSE
                -- Case 2: Time-limited -> must be active now
                OR (bl.battle_time_limited = TRUE
                    AND bl.start_date::date <= NOW()::date
                    AND bl.end_date::date >= NOW()::date)
              )
    ),
    ques_stats AS (
        SELECT bqm.battle_id,
               SUM(bqm.no_of_ques)::INT AS total_questions,
               SUM(bqm.no_of_ques * qd.xp_gained)::INT AS max_xp,
               SUM(bqm.no_of_ques * bqm.time_per_question) * INTERVAL '1 second' AS duration
        FROM "BattleQuesDifficultyMap" bqm
        JOIN "QuestionDifficulty" qd ON bqm.que_difficulty_id = qd.id
        WHERE bqm.is_deleted = FALSE
        GROUP BY bqm.battle_id
    ),
    participant_stats AS (
        SELECT battle_id, COUNT(*)::INT AS participants
        FROM "BattleStatus"
        WHERE is_deleted = FALSE
        GROUP BY battle_id
    ),
    user_status AS (
        SELECT bs.battle_id,
               BOOL_OR(bs.battle_status = 3) AS is_running,
               BOOL_OR(bs.battle_status IN (1,2)) AS is_completed
        FROM "BattleStatus" bs
        WHERE (bs.user1_id = p_user_id OR bs.user2_id = p_user_id)
          AND bs.is_deleted = FALSE
        GROUP BY bs.battle_id
    )
    SELECT ab.battle_id AS "BattleId",
           ab.battle_name AS "BattleName",
           ab.category_name AS "Category",
           ab.difficulty_name AS "Difficulty",
           ab.description AS "Description",
           COALESCE(qs.max_xp, 0) AS "MaxXP",
           COALESCE(qs.total_questions, 0) AS "TotalQuestions",
           COALESCE(qs.duration, INTERVAL '0') AS "Duration",
           COALESCE(ps.participants, 0) AS "Participants",
           COALESCE(us.is_running, FALSE) AS "IsBattleRunning"
    FROM active_battles ab
    LEFT JOIN ques_stats qs ON ab.battle_id = qs.battle_id
    LEFT JOIN participant_stats ps ON ab.battle_id = ps.battle_id
    LEFT JOIN user_status us ON ab.battle_id = us.battle_id
    WHERE COALESCE(us.is_completed, FALSE) = FALSE
    ORDER BY ab.battle_id ASC;
END;
$$ LANGUAGE plpgsql;