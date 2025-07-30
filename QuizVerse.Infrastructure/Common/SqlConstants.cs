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
}
