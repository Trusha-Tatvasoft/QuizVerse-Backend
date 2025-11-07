using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuizCommentSectionService
{
    public Task<int> TotalCommentsByQuizId(int quizId);
    public Task<QuizCommentsResponseDto> GetCommentsByQuizId(int quizId, int batchNumber);
}