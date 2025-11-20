-- ==============================================================================
-- Author:       <Brjrajsinh Jadeja>
-- Create date:  <4-September-2025>
-- Description:  <Fetches quizzes with support for searching, filtering and sorting.
--               The function applies filters like category, difficulty level,
--               tags, price range, rating range, and total time.
--               Sorting options include MostPopular, HighestRated, Newest,
--               and PriceLowToHigh. The function also returns a flag indicating
--               whether more data is available (has_more).>
-- Usage:        SELECT * FROM browse_quizzes(
--                   p_search_text              := 'science',
--                   p_quiz_category_id         := 2,
--                   p_quiz_difficulty_level_id := NULL,
--                   p_tag_ids                  := ARRAY[1, 3],
--                   p_sort_by                  := 'HighestRated',
--                   p_filter_by_type           := 'Premium',
--                   p_batch_number             := 1,
--                   p_min_price                := 0,
--                   p_max_price                := 200,
--                   p_min_rating               := 2,
--                   p_max_rating               := 5,
--                   p_min_total_time           := 5,
--                   p_max_total_time           := 120
--               );
-- ==============================================================================
 
CREATE OR REPLACE FUNCTION browse_quizzes(
    p_user_id INT,
    p_search_text TEXT DEFAULT NULL,
    p_quiz_category_id INT DEFAULT NULL,
    p_quiz_difficulty_level_id INT DEFAULT NULL,
    p_tag_ids INT[] DEFAULT NULL,
    p_sort_by TEXT DEFAULT NULL,
    p_filter_by_type TEXT DEFAULT NULL,
    p_batch_number INT DEFAULT 1,
    p_min_price NUMERIC DEFAULT 0,
    p_max_price NUMERIC DEFAULT NULL,
    p_min_rating NUMERIC DEFAULT 0,
    p_max_rating NUMERIC DEFAULT 5,
    p_min_total_time NUMERIC DEFAULT 2,
    p_max_total_time NUMERIC DEFAULT 180
)
RETURNS TABLE (
    quizzes JSON,
    "hasMore" BOOLEAN,
    "totalFeatured" INT,
    "totalFree" INT,
    "totalPremium" INT,
    "totalAll" INT
)
AS $$
BEGIN
    RETURN QUERY
    WITH base_all AS (  
        SELECT
            q.id,
            q.name,
            q.description,
            q.is_paid AS "isPaid",
            q.price,
            qc.category_name AS "categoryName",
            qd.name AS "difficultyLevel",
            (q.is_featured = TRUE AND qc.created_date >= NOW() - INTERVAL '30 days') AS "isFeatured",
            ARRAY_AGG(DISTINCT qt.tag_name) AS tags,
            q.total_time AS "totalTime",
            q.total_question AS "totalQuestions",
            COALESCE(
                COUNT(DISTINCT qa.id) FILTER (
                    WHERE u.is_deleted = FALSE
                ),
                0
            ) AS "totalParticipates",
            COALESCE(AVG(q.rating), 0) AS rating,
            EXISTS (
                SELECT 1
                FROM "QuizAttempted" qa2
                WHERE qa2.quiz_id = q.id
                  AND qa2.user_id = p_user_id
            ) AS "isAttempted",
            COALESCE((
			    SELECT jsonb_build_object(
			        'reportId', qir2.id,
			        'status', qir2.status,
			        'severity', qir2.severity,
                    'reportReason', qir2.reason,
			        'isEditable',
			            CASE 
			                WHEN qir2.severity = 4 OR qir2.status = 4 THEN false 
			                ELSE true 
			            END
			    )
			    FROM "QuizIssueReports" qir2
			    WHERE qir2.quiz_id = q.id 
			      AND qir2.user_id = p_user_id
			      AND qir2.status NOT IN (1, 2)
			    LIMIT 1
			), '{}'::jsonb) AS "report"

        FROM "Quiz" q
        INNER JOIN "QuizCategory" qc ON qc.id = q.category_id
        INNER JOIN "QuizDifficulty" qd ON qd.id = q.difficulty_level_id
        LEFT JOIN "QuizTagMapping" qtm ON qtm.quiz_id = q.id AND qtm.is_deleted = FALSE
        LEFT JOIN "QuizTag" qt ON qt.id = qtm.tag_id
        LEFT JOIN "QuizAttempted" qa ON qa.quiz_id = q.id
        LEFT JOIN "Users" u ON u.id = qa.user_id
        WHERE q.is_deleted = FALSE
          AND q.quiz_type = 1
          AND q.status = 1
          AND (p_search_text IS NULL OR q.name ILIKE '%' || p_search_text || '%' OR q.description ILIKE '%' || p_search_text || '%')
          AND (p_quiz_category_id IS NULL OR q.category_id = p_quiz_category_id)
          AND (p_quiz_difficulty_level_id IS NULL OR q.difficulty_level_id = p_quiz_difficulty_level_id)
          AND (p_tag_ids IS NULL OR EXISTS (
                SELECT 1
                FROM "QuizTagMapping" qtm2
                WHERE qtm2.quiz_id = q.id
                  AND qtm2.tag_id = ANY(p_tag_ids)
                  AND qtm2.is_deleted = FALSE
          ))
          AND (p_min_price IS NULL OR q.price >= p_min_price)
          AND (p_max_price IS NULL OR q.price <= p_max_price)
          AND (p_min_rating IS NULL OR q.rating >= p_min_rating)
          AND (p_max_rating IS NULL OR q.rating <= p_max_rating)
          AND (p_min_total_time IS NULL OR q.total_time >= p_min_total_time)
          AND (p_max_total_time IS NULL OR q.total_time <= p_max_total_time)
        GROUP BY q.id, qc.category_name, qd.name, q.is_featured, qc.created_date
    ),
    base AS (  
        SELECT *
        FROM base_all
        WHERE (p_filter_by_type IS NULL OR
              (p_filter_by_type = 'Premium' AND "isPaid" = TRUE) OR
              (p_filter_by_type = 'Free' AND "isPaid" = FALSE) OR
              (p_filter_by_type = 'Featured' AND "isFeatured" = TRUE))
    ),
    total_count AS (
        SELECT COUNT(*)::INT AS cnt FROM base
    ),
    totals AS (  
        SELECT
            COUNT(*) FILTER (WHERE b."isFeatured")::INT AS "totalFeatured",
            COUNT(*) FILTER (WHERE b."isPaid" = FALSE)::INT AS "totalFree",
            COUNT(*) FILTER (WHERE b."isPaid" = TRUE)::INT AS "totalPremium",
            COUNT(*)::INT AS "totalAll"
        FROM base_all b
    ),
    paged AS (
        SELECT b.*
        FROM base b
        ORDER BY
            CASE WHEN p_sort_by = 'MostPopular' THEN b."totalParticipates" END DESC,
            CASE WHEN p_sort_by = 'HighestRated' THEN b.rating END DESC,
            CASE WHEN p_sort_by = 'Newest' THEN b.id END DESC,
            CASE WHEN p_sort_by = 'PriceLowToHigh' THEN b.price END ASC,
            b.id ASC
        LIMIT 4
        OFFSET (p_batch_number - 1) * 4
    )
    SELECT
        (SELECT COALESCE(json_agg(p), '[]'::json) FROM paged p) AS quizzes,
        (t.cnt > (p_batch_number * 4)) AS "hasMore",
        totals."totalFeatured",
        totals."totalFree",
        totals."totalPremium",
        totals."totalAll"
    FROM total_count t, totals;
END;
$$ LANGUAGE plpgsql;