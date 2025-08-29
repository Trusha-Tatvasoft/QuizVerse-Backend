using AutoMapper;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class QuestionDifficultyService(IGenericRepository<QuestionDifficulty> _questionDifficultyRepository, IMapper _mapper) : IQuestionDifficultyService
{
    #region Get Battle Question Difficulty Data
    public async Task<List<QuestionDifficultyXPData>> GetBattleQuestionDifficultyData()
    {
        return _mapper.Map<List<QuestionDifficultyXPData>>(
            (await _questionDifficultyRepository.GetAllAsync()).Where(q => !q.IsDeleted)
        );
    }
    #endregion
}
