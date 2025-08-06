using AutoMapper;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class QuizDifficultyLevelService(IGenericRepository<QuizDifficulty> quizDifficultyRepository, IMapper mapper) : IQuizDifficultyLevelService
{
    public async Task<List<QuizDifficultyDTO>> GetQuizDifficultyList()
    {
        List<QuizDifficulty> quizDifficultiesList = [.. (await quizDifficultyRepository.GetAllAsync()).Where(d => !d.IsDeleted)];

        return mapper.Map<List<QuizDifficultyDTO>>(quizDifficultiesList);
    }


    public List<CommonListDropDownDto> GetAllQuizDifficulties()
    {
        IQueryable<QuizDifficulty> quizDifficulties = quizDifficultyRepository.GetQueryableInclude().Where(u => !u.IsDeleted);
        
        return mapper.ProjectTo<CommonListDropDownDto>(quizDifficulties).ToList();
    }
}
