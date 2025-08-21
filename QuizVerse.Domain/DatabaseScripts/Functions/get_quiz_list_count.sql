-- =============================================
-- Author:       Bhumi Shah
-- Create Date:  13-August-2025
-- Description:  Returns the total count of quizzes based on filtering criteria, including:
--                 • Filtering by search term (quiz title, category name, difficulty level)
--                 • Filtering by quiz status
--                 • Filtering by category and difficulty
--                 • Only non-deleted quizzes and associated category,quiz attempted user included
-- Usage:        SELECT get_quiz_list_count(
--                              p_search_term := 'math',
--                              p_quiz_status := NULL,
--                              p_category_id := 2,
--                              p_difficulty_id := NULL,
--                              p_quiz_type := 1
--                          );
-- =============================================

CREATE OR REPLACE FUNCTION get_quiz_list_count(
    p_search_term        TEXT DEFAULT NULL,
    p_quiz_status        INT DEFAULT NULL,
    p_category_id        INT DEFAULT NULL,
    p_difficulty_id      INT DEFAULT NULL,
    p_quiz_type          INT DEFAULT 1
)
RETURNS TABLE (
    total_records INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT COUNT(*)::INT
    FROM "Quiz" q
    JOIN "QuizCategory" c       ON c.id = q.category_id
    JOIN "QuizDifficulty" d     ON d.id = q.difficulty_level_id
    WHERE q.is_deleted = FALSE
      AND q.quiz_type = p_quiz_type
      AND c.is_deleted = FALSE
      AND (
            p_search_term IS NULL
            OR q.name              ILIKE '%' || p_search_term || '%'
            OR c.category_name     ILIKE '%' || p_search_term || '%'
            OR d.name              ILIKE '%' || p_search_term || '%'
          )
      AND (p_quiz_status   IS NULL OR q.status              = p_quiz_status)
      AND (p_category_id   IS NULL OR q.category_id         = p_category_id)
      AND (p_difficulty_id IS NULL OR q.difficulty_level_id = p_difficulty_id);
END;
$$;