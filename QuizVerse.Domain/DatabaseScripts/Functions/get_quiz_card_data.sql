-- =============================================
-- Author:       Bhumi Shah
-- Create Date:  13-August-2025
-- Description:  Returns summary counts for quizzes, including:
--                 • Total quizzes
--                 • Active quizzes (based on p_active_status)
--                 • Total participants
--                 • Total questions across quizzes
--                 • Only non-deleted quizzes, users, and question mappings
-- Usage:        SELECT * FROM get_quiz_card_data(
--                              p_active_status := 1
--                          );
-- =============================================

CREATE OR REPLACE FUNCTION get_quiz_card_data(p_active_status INT DEFAULT NULL)
RETURNS TABLE(
    "TotalQuiz" BIGINT,
    "ActiveQuiz" BIGINT,
    "TotalParticipants" BIGINT,
    "TotalQuestions" BIGINT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        COUNT(DISTINCT q.id) AS "TotalQuiz",  
        COUNT(DISTINCT CASE WHEN q.status = p_active_status THEN q.id END) AS "ActiveQuiz", 
        COUNT(DISTINCT qa.user_id) AS "TotalParticipants",  
        COUNT(DISTINCT qbm.que_id) AS "TotalQuestions"  
    FROM "Quiz" q
    LEFT JOIN "QuizAttempted" qa ON qa.quiz_id = q.id
    LEFT JOIN "Users" u ON u.id = qa.user_id AND u.is_deleted = false
    LEFT JOIN "QuizToBaseQuestionMap" qbm ON qbm.quiz_id = q.id AND qbm.is_deleted = false
    WHERE q.is_deleted = false;
END;
$$;