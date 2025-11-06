-- =============================================
-- Author:       Bhumi Shah
-- Create Date:  04-Nov-2025
-- Description:  Returns paginated Question Issue Reports for Content Moderation
-- Usage: SELECT * FROM get_content_moderation_question_report_list(1,10);
-- =============================================

CREATE OR REPLACE FUNCTION get_content_moderation_question_report_list(
    p_page_number      INT DEFAULT 1,
    p_page_size        INT DEFAULT 10,
    p_sort_column      VARCHAR DEFAULT NULL,
    p_sort_descending  BOOLEAN DEFAULT FALSE,
    p_severity         INT DEFAULT NULL,
    p_status           INT DEFAULT NULL
)
RETURNS TABLE (
    "Id"           INT,
    "QuestionId"   INT,
    "Question"     VARCHAR,
    "Creator"      VARCHAR,
    "Reporter"     VARCHAR,
    "Reason"       VARCHAR,
    "Severity"     INT,
    "Status"       INT,
    "MarkAsReviewBy" INT,
    "CreatedDate"  TIMESTAMP WITH TIME ZONE
)
LANGUAGE plpgsql
AS $$
DECLARE
    sort_dir VARCHAR;
    sort_col VARCHAR;
BEGIN
    sort_col := LOWER(COALESCE(NULLIF(p_sort_column, ''), 'id'));

    sort_col := CASE sort_col
        WHEN 'question'     THEN 'q.que_text'
        WHEN 'creator'      THEN 'creator.full_name'
        WHEN 'reporter'     THEN 'reporter.full_name'
        WHEN 'severity'     THEN 'r.severity'
        WHEN 'status'       THEN 'r.status'
        WHEN 'createddate'  THEN 'r.created_date'
        ELSE 'r.id'
    END;

    sort_dir := CASE WHEN p_sort_descending THEN 'DESC' ELSE 'ASC' END;

    RETURN QUERY EXECUTE format($f$
        SELECT
            r.id AS "Id",
            r.question_id AS "QuestionId",
            q.que_text AS "Question",
            creator.full_name AS "Creator",
            reporter.full_name AS "Reporter",
            r.description AS "Reason",
            r.severity AS "Severity",
            r.status AS "Status",
		    r.modified_by AS "MarkAsReviewBy",
            r.created_date AS "CreatedDate"
        FROM "QuestionIssueReports" r
        JOIN "BaseQuestions" q ON q.id = r.question_id
        JOIN "Users" creator ON creator.id = q.created_by
        JOIN "Users" reporter ON reporter.id = r.user_id
        WHERE r.is_deleted = FALSE
          AND ($3 IS NULL OR r.severity = $3)
          AND ($4 IS NULL OR r.status = $4)
        ORDER BY %s %s, r.id ASC
        LIMIT $1
        OFFSET ($2 - 1) * $1
    $f$, sort_col, sort_dir)
    USING p_page_size, p_page_number, p_severity, p_status;
END;
$$;
