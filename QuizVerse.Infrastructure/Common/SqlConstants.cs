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
    public const string GET_QUESTION_POOL_LIST_QUERY_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}}, {{6}})";
    #endregion

    #region QuizCategory
    public const string FN_CREATE_OR_UPDATE_QUIZ_CATEGORY = "SELECT * FROM fn_create_or_update_quiz_category(@p_id, @p_category_name, @p_description, @p_icon, @p_user_id)";
    #endregion

    #region QuizManagement
    public const string GET_QUIZ_CARD_DATA_FUNCTION = "get_quiz_card_data";
    public const string GET_QUIZ_CARD_DATA_QUERY_TEMPLATE = "SELECT * FROM {0}({{0}})";
    public const string GET_QUIZ_LIST_FUNCTION = "get_quiz_list";
    public const string GET_QUIZ_LIST_QUERY_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}}, {{6}}, {{7}})";
    public const string GET_QUIZ_LIST_COUNT_FUNCTION = "get_quiz_list_count";
    public const string GET_QUIZ_LIST_COUNT_QUERY_TEMPLATE = "SELECT * FROM {0}({{0}}, {{1}}, {{2}}, {{3}})";
    #endregion

}
