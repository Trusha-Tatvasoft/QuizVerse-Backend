-- ==============================================================================
-- Author:       <Devisha Gajjar>
-- Create date:  <31-October-2025>
-- Description:  <Fetches a single flagged quiz comment by its ID along with quiz category and author details>
-- Usage:        <SELECT * FROM get_flagged_comment_by_id(8);>
-- ==============================================================================

CREATE OR REPLACE FUNCTION get_flagged_comment_by_id(
    p_id INTEGER
)
RETURNS TABLE (
    "Id" INT,
    "Comment" TEXT,
    "Author" TEXT,
	"UserId" INT,
    "QuizName" TEXT,
    "QuizCategory" TEXT,
    "Reason" TEXT,
    "Date" TIMESTAMP WITH TIME ZONE,
    "Status" INT,
    "ModifiedBy" INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        qr.id AS "Id",
        qr.feedback::text AS "Comment",
        u.user_name::text AS "Author",
		u.id AS "UserId",
        q.name::text AS "QuizName",
        qc.category_name::text AS "QuizCategory",
        qr.reason::text AS "Reason",
        qr.created_date AS "Date",
        qr.status AS "Status",
        qr.modified_by AS "ModifiedBy"
    FROM public."QuizRating" qr
    INNER JOIN public."Users" u ON qr.user_id = u.id
    INNER JOIN public."Quiz" q ON qr.quiz_id = q.id
    INNER JOIN public."QuizCategory" qc ON q.category_id = qc.id
    WHERE qr.id = p_id
      AND qr.is_flagged = TRUE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Flagged comment not found or not flagged.'
            USING ERRCODE = 'P0001';
    END IF;
END;
$$;
