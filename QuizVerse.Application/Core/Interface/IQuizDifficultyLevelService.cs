using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuizDifficultyLevelService
{
    Task<List<QuizDifficultyDTO>> GetQuizDifficultyList();
    Task<QuizDifficultyDTO> GetDifficultyLevelById(int id);
    Task<string> CreateDifficultyLevel(QuizDifficultyRequestDto difficultyRequestDto);
    Task<bool> IsDifficultyNameAvailable(string name);  
}
