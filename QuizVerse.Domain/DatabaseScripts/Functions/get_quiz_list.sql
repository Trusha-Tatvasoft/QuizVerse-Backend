-- =============================================
-- Author:       Bhumi Shah
-- Create Date:  13-August-2025
-- Description:  Returns a paginated list of quizzes with metadata, including:
--                 • Quiz title, category, difficulty, number of questions, attempts, status, created date
--                 • Filtering by search term (matches title, category, or difficulty level)
--                 • Filtering by quiz status, category ID, and difficulty ID
--                 • Sorting by configurable column and direction (ascending/descending)
--                 • Pagination support (Page Number & Page Size)
--                 • Only non-deleted quizzes and associated category,quiz attempted user included
-- Usage:        SELECT * FROM get_quiz_list(
--                              p_page_number := 1,
--                              p_page_size := 10,
--                              p_search_term := 'math',
--                              p_sort_column := 'total_question',
--                              p_sort_descending := TRUE,
--                              p_quiz_status := NULL,
--                              p_category_id := 2,
--                              p_difficulty_id := NULL,
--                              p_quiz_type := 1
--                          );
-- Notes:        Valid p_sort_column values: quiz_title, category_name, quiz_difficulty_level, 
--               no_of_person_attempted, total_question, status, created_date
-- =============================================

CREATE OR REPLACE FUNCTION get_quiz_list(
    p_page_number        INT DEFAULT 1,
    p_page_size          INT DEFAULT 10,
    p_search_term        TEXT DEFAULT NULL,
    p_sort_column        TEXT DEFAULT NULL,   
    p_sort_descending    BOOLEAN DEFAULT FALSE,
    p_quiz_status        INT DEFAULT NULL,
    p_category_id        INT DEFAULT NULL,
    p_difficulty_id      INT DEFAULT NULL,
	p_quiz_type     INT DEFAULT 1 
)
RETURNS TABLE (
    "Id"                   INT,
    "QuizTitle"            VARCHAR,
    "CategoryName"         VARCHAR,
    "QuizDifficultyLevel"  VARCHAR,
    "TotalQuestion"        INT,
    "NoOfPersonAttempted"  INT,
    "Status"               INT,
    "CreatedDate"          TIMESTAMP
)
LANGUAGE plpgsql
AS $$
DECLARE
    sort_dir TEXT;
    sort_col TEXT;
BEGIN
	sort_col := COALESCE(NULLIF(p_sort_column, ''), 'id');
    sort_dir := CASE WHEN p_sort_descending THEN 'DESC' ELSE 'ASC' END;

    RETURN QUERY EXECUTE format($f$
        WITH base AS (
            SELECT
              q.id,
              q.name AS quiz_title,                
              c.category_name AS category_name,    
              d.name AS quiz_difficulty_level,     
              q.total_question,
              (
                SELECT COUNT(DISTINCT qa.user_id)::INT 
                FROM "QuizAttempted" qa
                JOIN "Users" u2
                  ON u2.id = qa.user_id
                 AND u2.is_deleted = FALSE
                WHERE qa.quiz_id = q.id
              ) AS no_of_person_attempted,
              q.status,
              q.created_date::TIMESTAMP,
              q.category_id,
              q.difficulty_level_id
            FROM "Quiz" q
            JOIN "QuizCategory" c ON c.id = q.category_id AND c.is_deleted = FALSE
            JOIN "QuizDifficulty" d ON d.id = q.difficulty_level_id
            WHERE q.is_deleted = FALSE AND ($7 IS NULL OR q.quiz_type = $7)
        )
        SELECT
            b.id,
            b.quiz_title,
            b.category_name,
            b.quiz_difficulty_level,
            b.total_question,
            b.no_of_person_attempted,
            b.status,
            b.created_date
        FROM base b
        WHERE
            ($1 IS NULL
            OR b.quiz_title            ILIKE '%%' || $1 || '%%'
            OR b.category_name         ILIKE '%%' || $1 || '%%'
            OR b.quiz_difficulty_level ILIKE '%%' || $1 || '%%')
            AND ($2 IS NULL OR b.status              = $2)
            AND ($3 IS NULL OR b.category_id         = $3)
            AND ($4 IS NULL OR b.difficulty_level_id = $4)
        ORDER BY %I %s, b.id ASC
        LIMIT $5
        OFFSET ($6 - 1) * $5
    $f$, sort_col, sort_dir)
    USING p_search_term, p_quiz_status, p_category_id, p_difficulty_id, p_page_size, p_page_number, p_quiz_type;
END;
$$;