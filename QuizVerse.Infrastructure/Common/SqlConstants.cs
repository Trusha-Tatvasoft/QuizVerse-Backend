namespace QuizVerse.Infrastructure.Common;

public class SqlConstants
{
    #region AdminDashboard
    public const string GET_DASHBOARD_METRICS = "SELECT * FROM get_admin_dashboard_metrics()";
    public const string GET_USER_ENGAGEMENT_CHART_DATA = "get_user_engagement_chart_data";
    public const string GET_REVENUE_TREND_CHART_DATA = "get_revenue_trend_chart_data";
    public const string GET_PERFORMANCE_SCORE_CHART_DATA = "get_performance_score_chart_data";
    public const string CHART_FUNCTION_CALL_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}})";
    #endregion

    #region User CRUD
    public const string CREATE_OR_UPDATE_USER_FUNCTION = "create_or_update_user";
    public const string CREATE_OR_UPDATE_USER_QUERY_TEMPLATE = "SELECT * FROM {0}(@p_id, @p_full_name, @p_email, @p_username, @p_password, @p_profile_pic, @p_bio, @p_role_id, @p_status_active, @p_status_inactive, @p_status_suspended, @p_modified_by, @p_first_time_login)";
    #endregion

    #region QuestionPool
    public const string GET_QUESTION_POOL_LIST_FUNCTION = "get_question_pool_list";
    public const string GET_QUESTION_POOL_TOTAL_COUNT_FUNCTION = "get_question_pool_total_count";

    public const string GET_QUESTION_POOL_LIST_QUERY_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}}, {{6}}, {{7}})";
    public const string GET_QUESTION_POOL_TOTAL_COUNT_QUERY_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}})";
    #endregion

    #region QuizCategory
    public const string CREATE_OR_UPDATE_QUIZ_CATEGORY = "SELECT * FROM create_or_update_quiz_category(@p_id, @p_category_name, @p_description, @p_icon, @p_user_id)";
    #endregion

    #region QuizManagement
    public const string GET_QUIZ_CARD_DATA_FUNCTION = "get_quiz_card_data";
    public const string GET_QUIZ_CARD_DATA_QUERY_TEMPLATE = "SELECT * FROM {0}({{0}},{{1}})";
    public const string GET_QUIZ_LIST_FUNCTION = "get_quiz_list";
    public const string GET_QUIZ_LIST_QUERY_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}}, {{6}}, {{7}}, {{8}})";
    public const string GET_QUIZ_LIST_COUNT_FUNCTION = "get_quiz_list_count";
    public const string GET_QUIZ_LIST_COUNT_QUERY_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}}, {{2}}, {{3}}, {{4}})";
    public const string CREATE_UPDATE_QUIZ_FUNCTION = "create_update_quiz";
    public const string GET_QUIZ_DATA_BY_ID_FUNCTION = "get_quiz_data_by_id";
    public const string UPDATE_QUIZ_ACTION_QUERY_FUNCTION = "update_quiz_action";

    public const string CREATE_UPDATE_QUIZ_QUERY_TEMPLATE =
        "SELECT * FROM {0}(" +
        "@p_quiz_id, @p_name, @p_category_id, @p_description, @p_total_time, " +
        "@p_difficulty_level_id, @p_total_question, @p_is_paid, @p_price, " +
        "@p_status, @p_tags, @p_questions, @p_no_of_questions_per_difficulty, @p_created_by)";
    public const string GET_QUIZ_DATA_BY_ID_QUERY_TEMPLATE =
        "SELECT * FROM {0}(@p_quiz_id)";
    public const string UPDATE_QUIZ_ACTION_QUERY_TEMPLATE =
        "SELECT * FROM {0}({{0}}, {{1}}, {{2}},{{3}})";
    #endregion

    #region BattleManagement
    public const string GET_BATTLE_LIST_TEMPLATE =
       "SELECT * FROM {0}({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}})";
    public const string GET_BATTLE_LIST_FUNCTION = "get_battles_list_data";

    public const string CREATE_UPDATE_BATTLE_FUNCTION = "create_update_battle";
    public const string CREATE_UPDATE_BATTLE_QUERY_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}}, {{6}}, {{7}}, {{8}}, {{9}}, {{10}}, {{11}}, {{12}}, {{13}}, {{14}}, {{15}})";

    public const string GET_BATTLE_DATA_BY_ID_QUERY_TEMPLATE =
     "SELECT * FROM {0}({{0}})";

    public const string GET_BATTLE_DATA_BY_ID_FUNCTION = "get_battle_data_by_id";

    #endregion

    #region UserDashboard
    public const string GET_USER_DASHBOARD_METRICS = "SELECT * FROM get_user_dashboard_metrics({0})";
    public const string GET_RANK_PROGRESS = "SELECT * FROM get_rank_progress({0})";
    #endregion

    #region LeaderBoard
    public const string GET_LEADERBOARD_QUERY_TEMPLATE = "SELECT * FROM {0}(@p_user_id)";
    public const string GET_CATEGORY_WISE_LEADERBOARD_QUERY_TEMPLATE = "SELECT * FROM {0}(@p_user_id, @p_category_id)";
    public const string GET_MONTHLY_CHAMPIONS_QUERY_TEMPLATE = "SELECT * FROM {0}(@p_user_id, @p_month, @p_year)";
    public const string GET_WEEKLY_LEADERBOARD_QUERY_TEMPLATE = "SELECT * FROM {0}(@p_user_id)";
    public const string GET_GLOBAL_LEADERBOARD_FUNCTION = "get_leaderboard_global_rankings";
    public const string GET_CATEGORY_WISE_LEADERBOARD_FUNCTION = "get_category_wise_leaderboard";
    public const string GET_MONTHLY_CHAMPIONS_FUNCTION = "get_monthly_champions";
    public const string GET_WEEKLY_LEADERBOARD_FUNCTION = "get_weekly_leaderboard";
    #endregion

    #region UserProfile
    public const string GET_USER_BASIC_PROFILE_FUNCTION = "get_user_basic_profile";
    public const string GET_USER_OVERVIEW_FUNCTION = "get_user_overview";
    public const string GET_USER_PROFILE_QUERY_TEMPLATE = "SELECT * FROM {0}(@p_user_id)";
    public const string UPDATE_USER_PROFILE_FUNCTION = "update_user_setting";
    public const string UPDATE_USER_PROFILE_QUERY_TEMPLATE = "SELECT * FROM {0}(@p_current_user_id, @p_new_email, @p_new_name, @p_new_bio)";
    public const string GET_USER_NAVBAR_FUNCTION = "get_user_navbar_data";
    public const string GET_USER_NAVBAR_QUERY_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}})";
    #endregion

    #region BrowseQuizzes
    public const string Browse_Quizzes_QUERY_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}}, {{6}}, {{7}}, {{8}}, {{9}}, {{10}}, {{11}}, {{12}})";
    public const string Browse_Quizzes_FUNCTION = "browse_quizzes";
    #endregion

    #region UserBattles
    public const string GET_USER_BATTLES_HISTORY_FUNCTION = "get_user_battles_history";
    public const string GET_USER_BATTLES_HISTORY_QUERY_TEMPLATE =
        "SELECT * FROM {0}(@p_user_id, @p_status_draw, @p_status_completed, @p_batch_number, @p_filter_by, @p_time_filter_by)";
    public const string GET_USER_BATTLE_LEADERBOARD_LIST_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}}, {{2}}, {{3}})";
    public const string GET_USER_BATTLE_LEADERBOARD_LIST_FUNCTION = "get_user_battles_leaderboard_list";
    public const string GET_USER_AVAILABLE_BATTLES_QUERY_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}})";
    public const string GET_USER_AVAILABLE_BATTLES_FUNCTION = "get_user_available_battle";
    public const string GET_Battle_QUESTIONS_QUERY_TEMPLATE = "SELECT * FROM get_battle_question({0}, {1})";
    public const string AFTER_Battle_RESULT = "SELECT * FROM after_battle_result({0})";
    #endregion

    #region Quiz
    public const string START_QUIZ_QUERY_TEMPLATE = "SELECT * FROM start_quiz({0}, {1})";
    public const string GET_QUIZ_QUESTIONS_QUERY_TEMPLATE = "SELECT * FROM get_quiz_questions({0}, {1})";
    public const string QUIZ_ATTEMPT_COMPLETE_FUNCTION = "SELECT * FROM quiz_attempt_complete({0}, {1}, {2})";
    public const string RECALC_USER_STREAK_FUNCTION = "SELECT * FROM recalc_user_streak({0})";
    public const string RECALC_GLOBAL_RANKS_FUNCTION = "SELECT * FROM recalc_global_ranks()";
    public const string CHECK_AND_AWARD_BADGES_FUNCTION = "SELECT * FROM check_and_award_badges({0})";
    public const string GET_QUIZ_QUESTION_REVIEW_FUNCTION = "SELECT * FROM get_quiz_question_review({0}, {1})";
    #endregion

    #region User Battles
    public const string GET_USER_BATTLE_RESULT_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}}, {{2}})";
    public const string GET_USER_BATTLE_RESULT_FUNCTION = "get_user_battle_result";
    #endregion

    #region Reports and Feedback
    public const string GET_PENDING_GCP_REPORTS_FUNCTION = "get_pending_gcp_reports";
    public const string GET_PENDING_GCP_REPORTS_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}}, {{2}})";
    #endregion

    #region Content Moderation
    public const string GET_CONTENT_MODERATION_METRICS = "SELECT * FROM get_content_moderation_metrics()";
    public const string GET_CONTENT_MODERATION_QUESTION_REPORT_LIST_FUNCTION = "get_content_moderation_question_report_list";
    public const string GET_CONTENT_MODERATION_QUESTION_REPORT_LIST_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}})";
    public const string GET_CONTENT_MODERATION_QUESTION_REPORT_TOTAL_COUNT_FUNCTION = "get_content_moderation_question_report_total_count";
    public const string GET_CONTENT_MODERATION_QUESTION_REPORT_TOTAL_COUNT_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}}, {{2}}, {{3}})";
    public const string GET_QUESTION_ISSUE_REPORT_PREVIEW_FUNCTION = "get_question_issue_report_preview";
    public const string GET_QUESTION_ISSUE_REPORT_PREVIEW_TEMPLATE = "SELECT * FROM {0}({{0}})";
    public const string GET_AFFECTED_QUIZ_AND_BATTLE_LIST_FUNCTION = "get_affected_quiz_and_battle_list";
    #endregion

    #region Flagged Comments
    public const string GET_FLAGGED_COMMENTS_QUERY_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}}, {{2}}, {{3}}, {{4}})";
    public const string GET_FLAGGED_COMMENTS_FUNCTION = "get_flagged_comments";
    public const string GET_FLAGGED_COMMENT_By_Id_QUERY_TEMPLATE = "SELECT * FROM {0}(@p_id)";
    public const string GET_FLAGGED_COMMENT_By_Id_FUNCTION = "get_flagged_comment_by_id";

    #endregion
}