-- =============================================
-- Author:       Darsh Aswani
-- Create Date:  05-August-2025
-- Description:  Returns the full list of questions from the pool, including:
--                 • Question metadata (Category, Difficulty, Type)
--                 • Nested options/answers per question as JSONB
--                 • Filtering by search term, category, difficulty, type
--                 • Sorting by configurable column and direction
--                 • Pagination support (Page Number & Page Size)
--                 • Only non-deleted questions and options
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

CREATE OR REPLACE FUNCTION get_question_pool_list(
    p_page_number INT DEFAULT 1,
    p_page_size INT DEFAULT 10,
    p_search_term TEXT DEFAULT NULL,
    p_sort_column TEXT DEFAULT '',
    p_sort_descending BOOLEAN DEFAULT FALSE,
    p_category_id INT DEFAULT NULL,
    p_difficulty_id INT DEFAULT NULL,
    p_question_type_id INT DEFAULT NULL
)
RETURNS TABLE (
    id INT,
    categoryId INT,
    categoryName VARCHAR,
    queDifficultyId INT,
    queDifficultyName VARCHAR,
    queText VARCHAR,
    queTypeId INT,
    queTypeName VARCHAR,
    queOptionsAns JSONB
)
AS $$
BEGIN
  RETURN QUERY
  SELECT
    bq.id,
    bq.category_id,
    qc.category_name,
    bq.que_difficulty_id,
    qd.name AS que_difficulty_name,
    bq.que_text,
    bq.que_type_id,
    qt.type_name,
    (
      SELECT jsonb_agg(
        jsonb_build_object(
          'id', qo.id,
          'questionId', qo.question_id,
          'key', qo.key,
          'value', qo.value
        )
      )
      FROM "QuestionOptionsAnswers" qo
      WHERE qo.question_id = bq.id AND qo.is_deleted = false
    ) AS que_options_ans
  FROM "BaseQuestions" bq
  JOIN "QuizCategory" qc ON bq.category_id = qc.id 
  JOIN "QuestionDifficulty" qd ON bq.que_difficulty_id = qd.id
  JOIN "QuestionType" qt ON bq.que_type_id = qt.id
  WHERE bq.is_deleted = false
    AND qc.is_deleted = false
    AND (p_search_term IS NULL OR bq.que_text ILIKE '%' || p_search_term || '%')
    AND (p_category_id IS NULL OR bq.category_id = p_category_id)
    AND (p_difficulty_id IS NULL OR bq.que_difficulty_id = p_difficulty_id)
    AND (p_question_type_id IS NULL OR bq.que_type_id = p_question_type_id)
  ORDER BY
    CASE
      WHEN p_sort_column = '' AND NOT p_sort_descending THEN bq.id
    END ASC,
    CASE 
      WHEN p_sort_column = '' AND p_sort_descending THEN bq.id
    END DESC,
    CASE
      WHEN p_sort_column = 'queText' AND NOT p_sort_descending THEN bq.que_text
      WHEN p_sort_column = 'categoryName' AND NOT p_sort_descending THEN qc.category_name
      WHEN p_sort_column = 'queDifficultyName' AND NOT p_sort_descending THEN qd.name
      WHEN p_sort_column = 'queTypeName' AND NOT p_sort_descending THEN qt.type_name
    ELSE NULL
    END ASC,
    CASE
      WHEN p_sort_column = 'queText' AND p_sort_descending THEN bq.que_text
      WHEN p_sort_column = 'categoryName' AND p_sort_descending THEN qc.category_name
      WHEN p_sort_column = 'queDifficultyName' AND p_sort_descending THEN qd.name
      WHEN p_sort_column = 'queTypeName' AND p_sort_descending THEN qt.type_name
    ELSE NULL
    END DESC
  LIMIT p_page_size
  OFFSET (p_page_number - 1) * p_page_size;
END;
$$ LANGUAGE plpgsql;
