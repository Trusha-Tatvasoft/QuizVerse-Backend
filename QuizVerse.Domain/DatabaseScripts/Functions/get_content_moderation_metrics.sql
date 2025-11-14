-- =============================================
-- Author:      <Zeel Vaghasiya>
-- Create date: <30-Oct-2025>
-- Description: <Get aggregated content moderation metrics
--               including pending reports, under review reports,
--               and resolved reports for today.>
-- Usage:       SELECT * FROM get_content_moderation_metrics();
-- =============================================
 
CREATE OR REPLACE FUNCTION get_content_moderation_metrics()
RETURNS TABLE (
    "PendingReportsCount" INT,
    "UnderReviewReportsCount" INT,
    "TodayResolvedReportsCount" INT,
	"BannedUserCount" INT
)
AS $$
BEGIN
    "PendingReportsCount" :=
        (
            -- Quiz Issue Reports (Pending)
            SELECT COUNT(*) FROM "QuizIssueReports" qir
            WHERE qir.status = 1
        ) +
        (
            -- Question Issue Reports (Pending)
            SELECT COUNT(*) FROM "QuestionIssueReports" q
            WHERE q.status = 1 AND q.is_deleted = false
        ) +
        (
            -- Flagged Quiz Ratings (Pending)
            SELECT COUNT(*) FROM "QuizRating" qr
            WHERE qr.is_flagged = true AND qr.status = 2
        );
 
    "UnderReviewReportsCount" :=
        (
            -- Quiz Issue Reports (Under Review)
            SELECT COUNT(*) FROM "QuizIssueReports" qir
            WHERE qir.status = 2
        ) +
        (
            -- Question Issue Reports (Under Review)
            SELECT COUNT(*) FROM "QuestionIssueReports" q
            WHERE q.status = 2 AND q.is_deleted = false
        );
 
    "TodayResolvedReportsCount" :=
        (
            -- Quiz Issue Reports (Resolved today)
            SELECT COUNT(*) FROM "QuizIssueReports" qir
            WHERE qir.status = 3
              AND qir.modified_date::date = CURRENT_DATE
        ) +
        (
            -- Question Issue Reports (Resolved today)
            SELECT COUNT(*) FROM "QuestionIssueReports" q
            WHERE q.status = 3
              AND q.is_deleted = false
              AND q.modified_date::date = CURRENT_DATE
        ) +
        (
            -- Flagged Quiz Ratings resolved today
            SELECT COUNT(*) FROM "QuizRating" qr
            WHERE qr.is_flagged = true
              AND qr.status = 1   -- Accepted
              AND qr.modified_date::date = CURRENT_DATE
        );

	"BannedUserCount" := 0;
 
    RETURN NEXT;
END;
$$ LANGUAGE plpgsql;