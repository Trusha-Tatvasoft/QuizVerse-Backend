using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuestionDifficultyService
{
    Task<List<QuestionDifficultyXPData>> GetBattleQuestionDifficultyData();
}
