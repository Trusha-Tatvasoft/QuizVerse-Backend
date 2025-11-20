namespace QuizVerse.Infrastructure.DTOs.ResponseDTOs;

public class ContentModerationMetricsDataDto
{
    public int PendingReportsCount { get; set; }
    public int UnderReviewReportsCount { get; set; }
    public int TodayResolvedReportsCount { get; set; }
    public int BannedUserCount { get; set; }
}
