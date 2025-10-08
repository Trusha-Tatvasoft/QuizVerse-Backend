-- =============================================
-- Author:       <Devisha Gajjar>
-- Create date:  <03-Sep-2025>
-- Description:  <Get recent battles of a user including
--               opponent, category, result, score, XP, profile pic,
--              with optional date filter, pagination, and HasMore flag>
-- Usage:        SELECT *
--                     FROM get_user_battles_history(
--                         2,  -- p_user_id: ID of the user
--                         2,  -- p_status_draw: Enum value for Draw
--                         1,  -- p_status_completed: Enum value for Completed
--                         1,  -- p_batch_number: Page number (pagination)
--                         3,  -- p_filter_by: 1=Draw, 2=Lost, 3=Won
--                         2   -- p_time_filter_by: 1=Last2Days, 2=Last7Days, 3=ThisMonth, 4=LastQuarter, 5=CurrentYeat, 6=LastYear
--                     );
-- =============================================
 
CREATE OR REPLACE FUNCTION get_user_battles_history(
    p_user_id INT,             -- ID of the current user
    p_status_draw INT,         -- Enum value for Draw (BattleStatus.Draw)
    p_status_completed INT,    -- Enum value for Completed (BattleStatus.Completed)
    p_batch_number INT DEFAULT 1,
    p_filter_by INT DEFAULT NULL,       -- 1=Draw,2=Lost,3=Won
    p_time_filter_by INT DEFAULT NULL   -- 1=Last2Days, 2=Last7Days, 3=ThisMonth, 4=LastQuarter, 5=CurrentYeat, 6=LastYear
)
RETURNS TABLE (
    battles JSONB,
    hasMore BOOLEAN
) AS $$
DECLARE
    v_page_size INT := 10;  
    v_offset    INT := (p_batch_number - 1) * v_page_size;
BEGIN
    RETURN QUERY
    WITH all_battles AS (
        SELECT
        -- Opponent name and profile pic
            q.name AS "BattleName",
            CASE WHEN bs.user1_id = p_user_id THEN u2.user_name ELSE u1.user_name END AS "Opponent",
            CASE WHEN bs.user1_id = p_user_id THEN u2.full_name ELSE u1.full_name END AS "OpponentFullName",
            CASE WHEN bs.user1_id = p_user_id THEN u2.profile_pic ELSE u1.profile_pic END AS "ProfilePic",
        -- Quiz category name
            qc.category_name AS "Category",
            CASE
 
        -- Battle result
               WHEN bs.battle_status = p_status_draw AND
                    ((bs.user1_id = p_user_id AND br.user1_corrected_ans = 0 AND br.user2_corrected_ans = 0) OR
                    (bs.user2_id = p_user_id AND br.user2_corrected_ans = 0 AND br.user1_corrected_ans = 0))
                    THEN 'Lost'
                WHEN bs.battle_status = p_status_draw THEN 'Draw'
                WHEN bs.battle_status = p_status_completed AND br.winner_id = p_user_id THEN 'Won'
                WHEN bs.battle_status = p_status_completed AND br.winner_id IS NOT NULL THEN 'Lost'
                ELSE 'Unknown'
            END AS "Result",
        -- Scores
            CASE WHEN bs.user1_id = p_user_id THEN br.user1_corrected_ans ELSE br.user2_corrected_ans END AS "YourScore",
            CASE WHEN bs.user1_id = p_user_id THEN br.user2_corrected_ans ELSE br.user1_corrected_ans END AS "OpponentScore",
           
 
        -- XP gained
            CASE
                WHEN bs.battle_status = p_status_draw THEN br.winner_gained_xp
                WHEN bs.battle_status = p_status_completed AND br.winner_id = p_user_id THEN br.winner_gained_xp
                WHEN bs.battle_status = p_status_completed AND br.winner_id IS NOT NULL THEN br.looser_gained_xp
                ELSE 0
            END AS "XpGained",
            bs.modified_date AS "BattleDate"
        FROM "BattleResult" br
        JOIN "BattleStatus" bs ON bs.id = br.battle_status
        JOIN "BattleList"  bl ON bs.battle_id = bl.id
        JOIN "Quiz" q  ON bl.quiz_id = q.id
        JOIN "QuizCategory" qc ON q.category_id = qc.id
        JOIN "Users" u1 ON bs.user1_id = u1.id
        JOIN "Users" u2 ON bs.user2_id = u2.id
        WHERE (bs.user1_id = p_user_id OR bs.user2_id = p_user_id)
          AND (bs.battle_status = p_status_draw OR bs.battle_status = p_status_completed)
          AND bl.is_deleted = FALSE
    ),
    filtered AS (
        SELECT *
        FROM all_battles
        WHERE (p_filter_by IS NULL OR
               (p_filter_by = 1 AND "Result" = 'Draw') OR
               (p_filter_by = 2 AND "Result" = 'Lost') OR
               (p_filter_by = 3 AND "Result" = 'Won'))
          AND (p_time_filter_by IS NULL OR
                 (p_time_filter_by = 1 AND "BattleDate" >= NOW() - INTERVAL '2 days') OR       -- Last2Days
                 (p_time_filter_by = 2 AND "BattleDate" >= NOW() - INTERVAL '7 days') OR       -- Last7Days
                 (p_time_filter_by = 3 AND "BattleDate" >= date_trunc('month', CURRENT_DATE)) OR -- CurrentMonth
                 (p_time_filter_by = 4 AND "BattleDate" >= NOW() - INTERVAL '3 months') OR      -- LastQuarter
                 (p_time_filter_by = 5 AND "BattleDate" >= date_trunc('year', CURRENT_DATE)) OR  -- CurrentYear
                 (p_time_filter_by = 6 AND "BattleDate" >= NOW() - INTERVAL '1 year')           -- LastYear
            )
        ORDER BY "BattleDate" DESC
    ),
    total_count AS (
        SELECT COUNT(*) AS cnt FROM filtered
    ),
    paged AS (
        SELECT *
        FROM filtered
        OFFSET v_offset
        LIMIT v_page_size
    )
    SELECT
        COALESCE(
            (SELECT jsonb_agg(row) FROM (SELECT * FROM paged) AS row),
            '[]'::jsonb
        ) AS battles,
        (SELECT cnt > v_offset + v_page_size FROM total_count) AS "hasMore";
END;
$$ LANGUAGE plpgsql;