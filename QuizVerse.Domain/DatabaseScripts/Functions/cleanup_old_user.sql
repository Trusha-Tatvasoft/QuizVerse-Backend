-- ==============================================================================
-- Author:       <Devisha Gajjar>
-- Create date:  <27-August-2025>
-- Description:  <Cleans up old user data by hard deleting sensitive/user-specific 
--               records, updating references to nullify relationships, and 
--               finally removing the old user record>
-- Usage:        CALL cleanup_old_user(p_old_user_id, p_modified_by);
-- ==============================================================================

CREATE OR REPLACE FUNCTION cleanup_old_user(
    p_old_user_id INT,
    p_modified_by INT DEFAULT NULL
)
RETURNS VOID AS $$
BEGIN
    -- Hard deletes
    DELETE FROM "UserPerformanceDetails" WHERE user_id = p_old_user_id;
    DELETE FROM "QuizPurchased" WHERE user_id = p_old_user_id;
    DELETE FROM "QuizAttempted" WHERE user_id = p_old_user_id;
    DELETE FROM "UserFavoriteQuizzes" WHERE user_id = p_old_user_id;
    DELETE FROM "QuestionIssueReports" WHERE user_id = p_old_user_id;
    DELETE FROM "QuizRating" WHERE user_id = p_old_user_id;
    DELETE FROM "UserNotifications" WHERE user_id = p_old_user_id;
    DELETE FROM "UserBadgesEarned" WHERE user_id = p_old_user_id;
    DELETE FROM "PasswordResetTokens" WHERE user_id = p_old_user_id;

    -- Soft cleanup
    UPDATE "BattleStatus"
    SET user1_id = CASE WHEN user1_id = p_old_user_id THEN 0 ELSE user1_id END,
        user2_id = CASE WHEN user2_id = p_old_user_id THEN 0 ELSE user2_id END,
        modified_date = CURRENT_TIMESTAMP,
        modified_by = COALESCE(p_modified_by, modified_by)
    WHERE user1_id = p_old_user_id OR user2_id = p_old_user_id;

    UPDATE "BattleResult"
    SET winner_id = CASE WHEN winner_id = p_old_user_id THEN 0 ELSE winner_id END
    WHERE winner_id = p_old_user_id;

    UPDATE "BattleRequest"
    SET sender_id = CASE WHEN sender_id = p_old_user_id THEN 0 ELSE sender_id END,
        receiver_id = CASE WHEN receiver_id = p_old_user_id THEN 0 ELSE receiver_id END,
        status = 4, -- cancelled
        modified_date = CURRENT_TIMESTAMP,
        modified_by = COALESCE(p_modified_by, modified_by)
    WHERE sender_id = p_old_user_id OR receiver_id = p_old_user_id;

    -- Finally, remove old user record
    DELETE FROM "Users" WHERE id = p_old_user_id;
END;
$$ LANGUAGE plpgsql;
