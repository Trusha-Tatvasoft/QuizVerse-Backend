using AutoMapper;
using Microsoft.AspNetCore.Http;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class QuizDifficultyLevelService(IGenericRepository<QuizDifficulty> quizDifficultyRepository, IMapper mapper, IHttpContextAccessor httpContextAccessor) : IQuizDifficultyLevelService
{
    public int UserId => httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

    #region Quiz Difficulty List
    public async Task<List<QuizDifficultyDTO>> GetQuizDifficultyList()
    {
        List<QuizDifficulty> quizDifficultiesList = [.. (await quizDifficultyRepository.GetAllAsync()).Where(d => !d.IsDeleted)];

        return mapper.Map<List<QuizDifficultyDTO>>(quizDifficultiesList);
    }
    #endregion

    #region Get by ID
    public async Task<QuizDifficultyDTO> GetDifficultyLevelById(int id)
    {
        QuizDifficulty data = await quizDifficultyRepository.GetAsync(u => u.Id == id && !u.IsDeleted)
            ?? throw new AppException(string.Format(Constants.DIFFICULTY_LEVEL_NOT_FOUND, id));

        return mapper.Map<QuizDifficultyDTO>(data);
    }
    #endregion

    #region Create difficulty level
    public async Task<string> CreateDifficultyLevel(QuizDifficultyRequestDto difficultyRequestDto)
    {
        await IsDifficultyNameAvailable(difficultyRequestDto.Name);

        var difficulty = mapper.Map<QuizDifficulty>(difficultyRequestDto);
        difficulty.CreatedDate = DateTime.UtcNow;
        difficulty.CreatedBy = UserId;

        await quizDifficultyRepository.AddAsync(difficulty);
        return Constants.CREATE_SUCCESS;
    }
    #endregion

    #region Difficulty Name Available
    public async Task<bool> IsDifficultyNameAvailable(string name)
    {
        if (await quizDifficultyRepository.Exists(u => u.Name.ToLower().Trim() == name.ToLower().Trim() && !u.IsDeleted))
            throw new AppException(Constants.DUPLICATE_DIFFICULTY_LEVEL_NAME);

        return true;
    }
    #endregion
}
