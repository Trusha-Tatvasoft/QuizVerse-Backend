using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuestionDifficultyService
{
    Task<List<QuestionDifficultyXPData>> GetBattleQuestionDifficultyData();
    public List<QuestionDifficultyResponseDTO> GetQuestionDifficulties();
    Task<string> AddOrEditQuestionDifficulty(QuestionDifficultyRequestDTO request);
    Task<string> DeleteQuestionDifficulty(int questionDifficultyId);
    Task<bool> IsQuestionDifficultyNameAvailable(string name);
    Task<bool> IsQuestionDifficultyXPAvailable(int xp);
}
