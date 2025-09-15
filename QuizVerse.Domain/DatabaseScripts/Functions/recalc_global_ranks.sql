-- =============================================
-- Author:      Zeel Vaghasiya
-- Create date: 05-September-2025
-- Description: Recalculates global ranks for all users based on total XP.
-- Usage:       select * from recalc_global_ranks();
-- Example:     select * from recalc_global_ranks(); -- Updates old and new global ranks
-- =============================================

CREATE OR REPLACE FUNCTION recalc_global_ranks()
RETURNS TABLE(success BOOLEAN) AS $$
BEGIN
    UPDATE "UserPerformanceDetails" u
    SET old_global_rank = new_global_rank,
        new_global_rank = CASE 
                              WHEN r.total_xp = 0 THEN 0
                              ELSE r.rnk
                          END,
        modified_date  = NOW()
    FROM (
        SELECT user_id, 
               total_xp,
               DENSE_RANK() OVER (ORDER BY total_xp DESC) AS rnk
        FROM "UserPerformanceDetails"
    ) r
    WHERE u.user_id = r.user_id;

    RETURN QUERY SELECT TRUE AS success;

EXCEPTION
    WHEN OTHERS THEN
        RETURN QUERY SELECT FALSE AS success;
END;
$$ LANGUAGE plpgsql;