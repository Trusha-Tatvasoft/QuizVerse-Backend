-- =============================================
-- Author:       Darsh Aswani
-- Create Date:  05-August-2025
-- Description: Returns the total number of questions in the pool that match the given filters, including:
--                 • Search by question text
--                 • Filter by category, difficulty, and question type
--                 • Only counts non-deleted questions
--                 • Ignores pagination and sorting (applies filters only)
-- Usage:        SELECT * FROM get_question_pool_list(
--                              p_page_number := 1,
--                              p_page_size := 10,
--                              p_search_term := '',
--                              p_sort_column := 'queTypeName',
--                              p_sort_descending := FALSE,
--                              p_category_id := NULL,
--                              p_difficulty_id := NULL,
--                              p_question_type_id := NULL
--                          );
-- =============================================

CREATE OR REPLACE FUNCTION get_question_pool_total_count(
    p_page_number INT DEFAULT 1,
    p_page_size INT DEFAULT 10,
    p_search_term TEXT DEFAULT NULL,
    p_category_id INT DEFAULT NULL,
    p_difficulty_id INT DEFAULT NULL,
    p_question_type_id INT DEFAULT NULL
)
RETURNS TABLE (
    total_records INT
)
AS $$
DECLARE
    v_total INT;
BEGIN
    SELECT COUNT(*)
    INTO v_total
    FROM "BaseQuestions" bq
    JOIN "QuizCategory" qc ON bq.category_id = qc.id
    JOIN "QuestionDifficulty" qd ON bq.que_difficulty_id = qd.id
    JOIN "QuestionType" qt ON bq.que_type_id = qt.id
    WHERE bq.is_deleted = false
      AND (p_search_term IS NULL OR bq.que_text ILIKE '%' || p_search_term || '%')
      AND (p_category_id IS NULL OR bq.category_id = p_category_id)
      AND (p_difficulty_id IS NULL OR bq.que_difficulty_id = p_difficulty_id)
      AND (p_question_type_id IS NULL OR bq.que_type_id = p_question_type_id);

    total_records := v_total;
    RETURN NEXT;
END;
$$ LANGUAGE plpgsql;
