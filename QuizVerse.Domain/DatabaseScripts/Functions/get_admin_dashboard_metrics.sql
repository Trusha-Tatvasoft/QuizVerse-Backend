-- =============================================
-- Author:		<Zeel Vaghasiya>
-- Create date: <28-July-2025>
-- Description:	<Get aggregated admin dashboard metrics including
--               user growth, active quizzes, revenue, and reports>
-- SELECT * FROM get_admin_dashboard_metrics();
-- =============================================

CREATE OR REPLACE FUNCTION get_admin_dashboard_metrics()
RETURNS TABLE (
    total_users INT,
    user_growth_percent NUMERIC,
    active_quizzes INT,
    quiz_growth_percent NUMERIC,
    total_revenue INT,
    revenue_growth_percent NUMERIC,
    total_reports INT,
    report_growth_percent NUMERIC
)
AS $$
DECLARE
    start_of_this_month TIMESTAMP := date_trunc('month', current_timestamp);
    start_of_last_month TIMESTAMP := start_of_this_month - interval '1 month';

    this_month_users INT;
    last_month_users INT;

    this_month_quizzes INT;
    last_month_quizzes INT;

    this_month_revenue INT;
    last_month_revenue INT;

    this_month_reports INT;
    last_month_reports INT;
BEGIN
    -- Users
    total_users := (
		SELECT COUNT(*)
		FROM "Users" 
		WHERE status = 1 AND is_deleted = false
	);
    this_month_users := (
		SELECT COUNT(*)
		FROM "Users"
		WHERE status = 1 AND is_deleted = false AND created_date >= start_of_this_month
	);
    last_month_users := (
		SELECT COUNT(*)
		FROM "Users"
		WHERE status = 1 AND is_deleted = false AND created_date >= start_of_last_month AND created_date < start_of_this_month
	);
    user_growth_percent := CASE
		WHEN last_month_users = 0 AND this_month_users = 0 THEN 0
		WHEN last_month_users = 0 THEN 100
		ELSE ROUND(((this_month_users - last_month_users) * 100.0 / last_month_users), 2)
	END;

    -- Quizzes
    active_quizzes := (
		SELECT COUNT(*)
		FROM "Quiz"
		WHERE status = 2 AND is_deleted = false
	);
    this_month_quizzes := (
		SELECT COUNT(*)
		FROM "Quiz"
		WHERE status = 2 AND is_deleted = false AND created_date >= start_of_this_month
	);
    last_month_quizzes := (
		SELECT COUNT(*)
		FROM "Quiz"
		WHERE status = 2 AND is_deleted = false AND created_date >= start_of_last_month AND created_date < start_of_this_month
	);
    quiz_growth_percent := CASE
		WHEN last_month_quizzes = 0 AND this_month_quizzes = 0 THEN 0
		WHEN last_month_quizzes = 0 THEN 100
		ELSE ROUND(((this_month_quizzes - last_month_quizzes) * 100.0 / last_month_quizzes), 2)
	END;

    -- Revenue
    total_revenue := COALESCE((
		SELECT SUM(payment_amount)
		FROM "GlobalPayment"
		WHERE payment_status = 2
	), 0);
    this_month_revenue := COALESCE((
		SELECT SUM(payment_amount)
		FROM "GlobalPayment"
		WHERE payment_status = 2 AND created_date >= start_of_this_month
	), 0);
    last_month_revenue := COALESCE((
		SELECT SUM(payment_amount)
		FROM "GlobalPayment"
		WHERE payment_status = 2 AND created_date >= start_of_last_month AND created_date < start_of_this_month
	), 0);
    revenue_growth_percent := CASE
		WHEN last_month_revenue = 0 AND this_month_revenue = 0 THEN 0
		WHEN last_month_revenue = 0 THEN 100
		ELSE ROUND(((this_month_revenue - last_month_revenue) * 100.0 / last_month_revenue), 2)
	END;

    -- Reports
    total_reports := (
		SELECT COUNT(*)
		FROM "QuestionIssueReports"
	);
    this_month_reports := (
		SELECT COUNT(*)
		FROM "QuestionIssueReports"
		WHERE created_date >= start_of_this_month
	);
    last_month_reports := (
		SELECT COUNT(*)
		FROM "QuestionIssueReports"
		WHERE created_date >= start_of_last_month AND created_date < start_of_this_month
	);
    report_growth_percent := CASE
		WHEN last_month_reports = 0 AND this_month_reports = 0 THEN 0
		WHEN last_month_reports = 0 THEN 100
		ELSE ROUND(((this_month_reports - last_month_reports) * 100.0 / last_month_reports), 2)
	END;

    RETURN NEXT;
END;
$$ LANGUAGE plpgsql;