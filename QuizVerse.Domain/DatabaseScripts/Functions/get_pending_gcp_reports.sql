-- =============================================
-- Author:        Bhumi Shah
-- Create date:   <31-10-2025>
-- Description:   <Fetch all pending GCP reports including
--                 quiz rating feedbacks, question issues,
--                 and quiz issue reports for processing>
-- Usage:         SELECT * FROM get_pending_gcp_reports();
-- =============================================
CREATE OR REPLACE FUNCTION get_pending_gcp_reports(
    p_quiz_rating_feedback_type INT DEFAULT 1,
    p_question_issue_report_type INT DEFAULT 2,
    p_quiz_issue_report_type INT DEFAULT 3
)
RETURNS TABLE (
    "ReportId" INT,
    "ReportType" INT,
    "ReportComment" VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    -- Quiz Rating Feedback reports
    RETURN QUERY
    SELECT qr.id AS "ReportId",
           p_quiz_rating_feedback_type AS "ReportType",  -- QuizRatingFeedback
           qr.feedback AS "ReportComment"
    FROM "QuizRating" qr
    WHERE qr.status = 4
      AND qr.feedback IS NOT NULL;

    -- Question Issue Reports
    RETURN QUERY
    SELECT qir.id AS "ReportId",
           p_question_issue_report_type AS "ReportType",  -- QuestionIssueReport
           qir.description AS "ReportComment"
    FROM "QuestionIssueReports" qir
    WHERE qir.severity = 4;

    -- Quiz Issue Reports
    RETURN QUERY
    SELECT qzr.id AS "ReportId",
           p_quiz_issue_report_type AS "ReportType",  -- QuizIssueReport
           qzr.reason AS "ReportComment"
    FROM "QuizIssueReports" qzr
    WHERE qzr.severity = 4;
END;
$$;