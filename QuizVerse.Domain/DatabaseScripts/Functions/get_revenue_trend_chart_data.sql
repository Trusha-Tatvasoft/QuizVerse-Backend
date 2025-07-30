-- =============================================
-- Author:		<Zeel Vaghasiya>
-- Create date: <28-July-2025>
-- Description:	<Get revenue trend chart data grouped dynamically
--               by day, month, or year based on the given date range>
-- SELECT * FROM get_revenue_trend_chart_data('2025-07-20', '2025-07-24');
-- =============================================

CREATE OR REPLACE FUNCTION get_revenue_trend_chart_data(start_date DATE, end_date DATE)
RETURNS TABLE(label TEXT, value INTEGER)
AS $$
DECLARE
    diff INTEGER := end_date - start_date;
    fmt TEXT;
BEGIN
    IF diff <= 31 THEN
        fmt := 'YYYY-MM-DD';  -- Group by Day
    ELSIF diff <= 366 THEN
        fmt := 'YYYY-MM';     -- Group by Month
    ELSE
        fmt := 'YYYY';        -- Group by Year
    END IF;

    RETURN QUERY
    SELECT 
        TO_CHAR(created_date, fmt) AS label,
        SUM(payment_amount)::INTEGER AS value
    FROM "GlobalPayment"
    WHERE 
        payment_status = 2 AND
        created_date::DATE BETWEEN start_date AND end_date
    GROUP BY label
    ORDER BY label;
END;
$$ LANGUAGE plpgsql;