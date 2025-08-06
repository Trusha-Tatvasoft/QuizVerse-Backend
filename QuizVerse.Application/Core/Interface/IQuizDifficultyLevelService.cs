using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IQuizDifficultyLevelService
{
    Task<List<QuizDifficultyDTO>> GetQuizDifficultyList();
    List<CommonListDropDownDto> GetAllQuizDifficulties();
}
