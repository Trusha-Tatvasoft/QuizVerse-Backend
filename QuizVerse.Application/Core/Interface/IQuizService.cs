using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuizService
{
    public Task<QuizOverviewResponseDto> GetQuizOverviewAsync(int quizId);
    public Task<QuizStartResponseDto?> StartQuizAsync(int quizId);
    public Task<QuizQuestionResponseDto?> SaveAndNextQuestion(SaveAndNextQuestionRequestDto request);
    public Task<bool> SubmitQuiz(SubmitQuizRequestDTO request);
    Task<QuizCompletedSummaryDTO> GetQuizSummary(int quizAttemptId);
    Task<List<QuizQuestionReviewDTO>> GetQuizQuestionReview(int quizId);
    Task<string> ReportQuestionIssue(QuestionIssueReportRequestDTO request);
    Task<QuizRatingDTO?> GetMyQuizRating(int quizId);
    Task<string> SubmitQuizRating(QuizRatingDTO request);
    Task<string> GetAnswerExplanation(AnswerExplanationRequestDTO request);
    Task<bool> AddEditQuizReport(QuizReportRequestDto quizReportRequestDto);
}