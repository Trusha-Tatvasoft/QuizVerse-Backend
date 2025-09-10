using AutoMapper;
using Microsoft.AspNetCore.Http;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class QuestionDifficultyService(IGenericRepository<QuestionDifficulty> _questionDifficultyRepository, IMapper _mapper, IHttpContextAccessor _httpContextAccessor, IDropDownDataService _dropDownDataService) : IQuestionDifficultyService
{
    int UserId => _httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.INVALID_USER_ID_MESSAGE);

    #region Get Battle Question Difficulty Data
    public async Task<List<QuestionDifficultyXPData>> GetBattleQuestionDifficultyData()
    {
        return _mapper.Map<List<QuestionDifficultyXPData>>(
            (await _questionDifficultyRepository.GetAllAsync()).Where(q => !q.IsDeleted)
        );
    }
    #endregion

    public List<QuestionDifficultyResponseDTO> GetQuestionDifficulties()
    {
        IQueryable<QuestionDifficulty> questionDifficulties = _questionDifficultyRepository.GetQueryableInclude(q => q.BaseQuestions).Where(q => !q.IsDeleted);
        List<QuestionDifficultyResponseDTO> response = _mapper.Map<List<QuestionDifficultyResponseDTO>>(questionDifficulties);

        foreach (QuestionDifficultyResponseDTO item in response)
        {
            item.TotalQuestions = questionDifficulties.First(q => q.Id == item.Id).BaseQuestions.Where(q => !q.IsDeleted).Count();
        }

        return response;
    }

    public async Task<string> AddOrEditQuestionDifficulty(QuestionDifficultyRequestDTO request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // EDIT case
        if (request.Id.HasValue && request.Id.Value > 0)
        {
            QuestionDifficulty existingEntity = await _questionDifficultyRepository
                .GetAsync(q => q.Id == request.Id && !q.IsDeleted)
                ?? throw new AppException(Constants.QUESTION_DIFFICULTY_NOT_FOUND, 404);

            // check if name is changed
            if (!string.Equals(existingEntity.Name, request.Name, StringComparison.OrdinalIgnoreCase))
            {
                // see if same name exists and is active
                QuestionDifficulty? conflict = await _questionDifficultyRepository.GetAsync(
                    q => q.Name.ToLower() == request.Name.ToLower() && !q.IsDeleted);

                if (conflict != null)
                    throw new AppException(String.Format(Constants.QUESTION_DIFFICULTY_ALREADY_EXISTS, request.Name));
            }

            // check if XP value conflicts with another record
            QuestionDifficulty? xpConflict = await _questionDifficultyRepository.GetAsync(
                q => q.XpGained == request.XpGainedPerQuestion && q.Id != request.Id && !q.IsDeleted);

            if (xpConflict != null)
                throw new AppException(string.Format(Constants.QUESTION_DIFFICULTY_DUPLICATE_XP, request.XpGainedPerQuestion));

            existingEntity.Name = request.Name;
            existingEntity.Description = request.Description;
            existingEntity.XpGained = request.XpGainedPerQuestion;
            existingEntity.ModifiedDate = DateTime.UtcNow;
            existingEntity.ModifiedBy = UserId;

            await _questionDifficultyRepository.UpdateAsync(existingEntity);
            _dropDownDataService.ClearCache(DropDownType.QuestionDifficulty);
            return Constants.QUESTION_DIFFICULTY_UPDATED;
        }
        else
        {
            // ADD case : check for duplicate xp
            QuestionDifficulty? xpConflict = await _questionDifficultyRepository.GetAsync(
            q => q.XpGained == request.XpGainedPerQuestion && !q.IsDeleted);

            if (xpConflict != null)
                throw new AppException(string.Format(Constants.QUESTION_DIFFICULTY_DUPLICATE_XP, request.XpGainedPerQuestion));

            // Check for duplicate name
            QuestionDifficulty? existingEntity = await _questionDifficultyRepository.GetAsync(
                q => q.Name.ToLower() == request.Name.ToLower());

            if (existingEntity != null)
            {
                if (!existingEntity.IsDeleted)
                    throw new AppException(String.Format(Constants.QUESTION_DIFFICULTY_ALREADY_EXISTS, request.Name));

                // Revive deleted one
                existingEntity.IsDeleted = false;
                existingEntity.Description = request.Description;
                existingEntity.XpGained = request.XpGainedPerQuestion;
                existingEntity.ModifiedDate = DateTime.UtcNow;
                existingEntity.ModifiedBy = UserId;

                await _questionDifficultyRepository.UpdateAsync(existingEntity);
                _dropDownDataService.ClearCache(DropDownType.QuestionDifficulty);
                return Constants.QUESTION_DIFFICULTY_ADDED;
            }

            // New add
            QuestionDifficulty newEntity = new QuestionDifficulty
            {
                Name = request.Name,
                Description = request.Description,
                XpGained = request.XpGainedPerQuestion,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = UserId,
                IsDeleted = false
            };

            await _questionDifficultyRepository.AddAsync(newEntity);
            _dropDownDataService.ClearCache(DropDownType.QuestionDifficulty);
            return Constants.QUESTION_DIFFICULTY_ADDED;
        }
    }

    public async Task<string> DeleteQuestionDifficulty(int questionDifficultyId)
    {
        QuestionDifficulty existingEntity = await _questionDifficultyRepository
                .GetAsync(q => q.Id == questionDifficultyId && !q.IsDeleted)
                ?? throw new AppException(Constants.QUESTION_DIFFICULTY_NOT_FOUND, 404);

        existingEntity.IsDeleted = true;
        existingEntity.ModifiedDate = DateTime.UtcNow;
        existingEntity.ModifiedBy = UserId;

        await _questionDifficultyRepository.UpdateAsync(existingEntity);
        _dropDownDataService.ClearCache(DropDownType.QuestionDifficulty);
        return Constants.QUESTION_DIFFICULTY_DELETED;
    }

    public async Task<bool> IsQuestionDifficultyNameAvailable(string name)
    {
        if (await _questionDifficultyRepository.Exists(u => u.Name.ToLower().Trim() == name.ToLower().Trim() && !u.IsDeleted))
            throw new AppException(Constants.QUESTION_DIFFICULTY_DUPLICATE_NAME);

        return true;
    }

    public async Task<bool> IsQuestionDifficultyXPAvailable(int xp)
    {
        if (await _questionDifficultyRepository.Exists(u => u.XpGained == xp && !u.IsDeleted))
            throw new AppException(Constants.QUESTION_DIFFICULTY_DUPLICATE_XP);

        return true;
    }
}
