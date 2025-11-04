using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuizCommentSectionService
{
    public Task<List<QuizCommentsDto>> GetCommentsByQuizId(int quizId);
}