using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class QuestionIssuePreviewRequestDto : QuestionReportData
{
    public QuestionDetailDTO QuestionDetail { get; set; } = null!;
}

public class QuestionReportData
{
    public int ActiveQuizContainCount { get; set; }
    public int ActiveBattleContainCount { get; set; }
}
