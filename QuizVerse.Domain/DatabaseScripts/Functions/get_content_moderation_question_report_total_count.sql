-- =============================================
-- Author:       Bhumi Shah
-- Create Date:  04-Nov-2025
-- Description:  Returns total count of Question Issue Reports for Content Moderation
-- Usage: SELECT * FROM get_content_moderation_question_report_total_count(1, 10, NULL, NULL);
-- =============================================

CREATE OR REPLACE FUNCTION get_content_moderation_question_report_total_count(
    p_page_number      INT DEFAULT 1,  
    p_page_size        INT DEFAULT 10,  
    p_severity         INT DEFAULT NULL,
    p_status           INT DEFAULT NULL
)
RETURNS TABLE (
    "total_records" INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        COUNT(*)::INT AS "total_records"
    FROM "QuestionIssueReports" r
    JOIN "BaseQuestions" q ON q.id = r.question_id
    JOIN "Users" creator ON creator.id = q.created_by
    JOIN "Users" reporter ON reporter.id = r.user_id
    WHERE r.is_deleted = FALSE
      AND (p_severity IS NULL OR r.severity = p_severity)
      AND (p_status IS NULL OR r.status = p_status);
END;
$$;
