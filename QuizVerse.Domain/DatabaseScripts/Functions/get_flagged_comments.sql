-- ==============================================================================
-- Author:       <Devisha Gajjar>
-- Create date:  <30-October-2025>
-- Description:  <Fetches flagged quiz comments with pagination, sorting, and optional status filter>
-- Usage:        <SELECT * FROM get_flagged_comments(p_page_number := 1, p_page_size := 10);>
-- ==============================================================================

CREATE OR REPLACE FUNCTION get_flagged_comments(
    p_page_number INTEGER DEFAULT 1,
    p_page_size INTEGER DEFAULT 10,
    p_sort_column VARCHAR DEFAULT 'id',
    p_sort_direction VARCHAR DEFAULT 'DESC',
    p_status_filter INTEGER DEFAULT NULL
)
RETURNS TABLE (
    "Records" JSONB,
    "TotalRecords" BIGINT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        COALESCE(
            jsonb_agg(to_jsonb(t)),
            '[]'::jsonb
        ) AS "Records",
        (
            SELECT COUNT(*)
            FROM public."QuizRating" qr
            WHERE qr.is_flagged = TRUE
              AND (p_status_filter IS NULL OR qr.status = p_status_filter)
        ) AS "TotalRecords"
    FROM (
        SELECT
            qr.id,
            qr.feedback AS "comment",
            u.user_name AS "author",
            q.name AS "quizName",
            qr.reason,
            qr.created_date AS "date",
            qr.status
        FROM public."QuizRating" qr
        INNER JOIN public."Users" u ON qr.user_id = u.id
        INNER JOIN public."Quiz" q ON qr.quiz_id = q.id
        WHERE qr.is_flagged = TRUE
          AND (p_status_filter IS NULL OR qr.status = p_status_filter)
        ORDER BY
            CASE WHEN p_sort_column = 'date' AND p_sort_direction = 'ASC' THEN qr.created_date END ASC,
            CASE WHEN p_sort_column = 'date' AND p_sort_direction = 'DESC' THEN qr.created_date END DESC,
            CASE WHEN p_sort_column = 'id' AND p_sort_direction = 'ASC' THEN qr.id END ASC,
            CASE WHEN p_sort_column = 'id' AND p_sort_direction = 'DESC' THEN qr.id END DESC,
            CASE WHEN p_sort_column = 'comment' AND p_sort_direction = 'ASC' THEN qr.feedback END ASC,
            CASE WHEN p_sort_column = 'comment' AND p_sort_direction = 'DESC' THEN qr.feedback END DESC,
            CASE WHEN p_sort_column = 'reason' AND p_sort_direction = 'ASC' THEN qr.reason END ASC,
            CASE WHEN p_sort_column = 'reason' AND p_sort_direction = 'DESC' THEN qr.reason END DESC,
            CASE WHEN p_sort_column = 'author' AND p_sort_direction = 'ASC' THEN u.user_name END ASC,
            CASE WHEN p_sort_column = 'author' AND p_sort_direction = 'DESC' THEN u.user_name END DESC,
            CASE WHEN p_sort_column = 'quizName' AND p_sort_direction = 'ASC' THEN q.name END ASC,
            CASE WHEN p_sort_column = 'quizName' AND p_sort_direction = 'DESC' THEN q.name END DESC,
            CASE WHEN p_sort_column = 'status' AND p_sort_direction = 'ASC' THEN qr.status END ASC,
            CASE WHEN p_sort_column = 'status' AND p_sort_direction = 'DESC' THEN qr.status END DESC,
            qr.id DESC; 
        LIMIT p_page_size
        OFFSET (p_page_number - 1) * p_page_size
    ) t;
END;
$$;