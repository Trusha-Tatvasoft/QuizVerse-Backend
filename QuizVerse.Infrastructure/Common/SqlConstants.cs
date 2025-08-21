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
    public const string DELETE_QUIZ_FUNCTION = "delete_quiz";

    public const string CREATE_UPDATE_QUIZ_QUERY_TEMPLATE =
        "SELECT * FROM {0}(" +
        "@p_quiz_id, @p_name, @p_category_id, @p_description, @p_total_time, " +
        "@p_difficulty_level_id, @p_total_question, @p_is_paid, @p_price, " +
        "@p_status, @p_tags, @p_questions, @p_no_of_questions_per_difficulty, @p_created_by)";
    public const string GET_QUIZ_DATA_BY_ID_QUERY_TEMPLATE =
        "SELECT * FROM {0}(@p_quiz_id)";
    public const string DELETE_QUIZ_QUERY_TEMPLATE =
        "SELECT * FROM {0}(@p_quiz_id, @p_modified_by)";
    #endregion

    #region BattleManagement
    public const string GET_BATTLE_LIST_TEMPLATE =
       "SELECT * FROM {0}({{0}}, {{1}}, {{2}})";
    public const string GET_BATTLE_LIST_FUNCTION = "get_battle_list_data";
    #endregion
}
