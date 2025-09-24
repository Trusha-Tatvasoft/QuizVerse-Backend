using System.Linq.Dynamic.Core;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.Enums;
using Npgsql;
using System.Data;
using QuizVerse.Infrastructure.Common.Exceptions;
using AutoMapper;

namespace QuizVerse.Application.Core.Service;

public class QuizCategoryService(IGenericRepository<QuizCategory> _quizCategoryRepository, IMapper _mapper, IHttpContextAccessor _httpContextAccessor, ISqlQueryRepository _sqlQueryRepository, IDropDownDataService dropDownDataService) : IQuizCategoryService
{

    private int UserId => _httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

    public async Task<PageListResponse<QuizCategoryDTO>> GetQuizCategories(PageListRequest pageListRequest)
    {
        IQueryable<QuizCategory> quizCategories = _quizCategoryRepository.GetQueryableInclude(q => q.Quizzes.Where(q => q.QuizType == (int)QuizType.Normal)).Where(q => !q.IsDeleted);
        // Search
        if (!string.IsNullOrWhiteSpace(pageListRequest.SearchTerm))
        {
            string term = pageListRequest.SearchTerm.ToLower();
            quizCategories = quizCategories.Where(u =>
                u.CategoryName.ToLower().Contains(term) ||
                u.Description.ToLower().Contains(term));
        }

        // Validate page number
        int totalRecords = quizCategories.Count();
        int maxPageNumber = (int)Math.Ceiling((double)totalRecords / pageListRequest.PageSize);
        if (pageListRequest.PageNumber > maxPageNumber && totalRecords > 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageListRequest.PageNumber), string.Format(Constants.INVALID_PAGE_NO, pageListRequest.PageNumber, maxPageNumber));
        }

        // Validate Sort Column
        if (!string.IsNullOrEmpty(pageListRequest.SortColumn))
        {

            bool columnExists = typeof(QuizCategory).GetProperty(
                 pageListRequest.SortColumn,
                 BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
             ) != null;
            if (pageListRequest.SortColumn.ToLower() == "quizcount")
            {
                pageListRequest.SortColumn = "Quizzes.Count()";
            }

            // Check if property exists
            var propertyInfo = typeof(QuizCategory).GetProperty(
                pageListRequest.SortColumn,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
            );

            if (propertyInfo == null && pageListRequest.SortColumn != "Quizzes.Count()")
            {
                throw new ArgumentException(
                    string.Format(Constants.INVALID_COLUMN_NAME, pageListRequest.SortColumn),
                    nameof(pageListRequest.SortColumn));
            }

            // If the property is boolean, invert the sort direction
            if (propertyInfo?.PropertyType == typeof(bool))
            {
                pageListRequest.SortDescending = !pageListRequest.SortDescending;
            }
            quizCategories = quizCategories.OrderBy($"{pageListRequest.SortColumn} {(pageListRequest.SortDescending ? "desc" : "asc")}");
        }
        else
        {
            quizCategories = quizCategories.OrderBy(q => q.Id);
        }

        PageListResponse<QuizCategory> pagedResult = await _quizCategoryRepository.PaginatedList<QuizCategory>(quizCategories, pageListRequest);
        List<QuizCategoryDTO> quizCategoryDtos = _mapper.Map<List<QuizCategoryDTO>>(pagedResult.Records);

        // Set QuizCount for each category
        foreach (QuizCategoryDTO dto in quizCategoryDtos)
        {
            QuizCategory? matchingEntity = pagedResult.Records.FirstOrDefault(q => q.Id == dto.Id);
            dto.QuizCount = matchingEntity?.Quizzes?.Count ?? 0;
        }

        return new PageListResponse<QuizCategoryDTO>
        {
            Records = quizCategoryDtos,
            TotalRecords = totalRecords,
        };
    }


    #region GetQuizCategoryById
    public async Task<QuizCategoryDTO> GetQuizCategoryById(int id)
    {
        QuizCategory quizCategory = await _quizCategoryRepository
            .GetQueryableInclude(c => c.Quizzes)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new AppException(string.Format(Constants.QUIZ_CATEGORY_NOT_FOUND, id));

        QuizCategoryDTO quizCategoryDTO = _mapper.Map<QuizCategoryDTO>(quizCategory);
        quizCategoryDTO.QuizCount = quizCategory.Quizzes?.Count ?? 0;

        return quizCategoryDTO;
    }
    #endregion

    #region  Create Or Update
    public async Task<(bool Success, string Message)> CreateOrUpdateQuizCategory(QuizCategoryDTO dto)
    {
        var parameters = new[]
        {
            new NpgsqlParameter("@p_id", dto.Id ?? (object)DBNull.Value),
            new NpgsqlParameter("@p_category_name", dto.CategoryName),
            new NpgsqlParameter("@p_icon", dto.Icon),
            new NpgsqlParameter("@p_description",dto.Description),
            new NpgsqlParameter("@p_user_id", UserId)
        };

        CreateUpdateResponseDto raw = await _sqlQueryRepository.SqlQuerySingleAsync<CreateUpdateResponseDto>(
            SqlConstants.CREATE_OR_UPDATE_QUIZ_CATEGORY,
            parameters
        );

        if (raw.Success)
        {
            dropDownDataService.ClearCache(DropDownType.QuizCategory);
        }

        return (raw.Success, raw.Message);
    }
    #endregion

    #region Update By Action
    public async Task<string> UpdateQuizCategoryByAction(QuizCategoryActionRequestDto quizCategoryAction)
    {
        QuizCategory quizCategory = await _quizCategoryRepository
            .GetAsync(q => q.Id == quizCategoryAction.Id && !q.IsDeleted)
            ?? throw new AppException(string.Format(Constants.QUIZ_CATEGORY_NOT_FOUND, quizCategoryAction.Id));

        string resultMessage;

        // Map the existing bool status to a readable string
        string existingStatus = quizCategory.Status ? "Active" : "Inactive";

        switch (quizCategoryAction.Action)
        {
            case QuizCategoryActionType.Delete:
                if (quizCategory.IsDeleted)
                {
                    throw new AppException(string.Format(Constants.USER_ALREADY_DELETED, quizCategoryAction.Id));
                }

                quizCategory.IsDeleted = true;
                quizCategory.ModifiedBy = UserId;
                quizCategory.ModifiedDate = DateTime.UtcNow;

                await _quizCategoryRepository.UpdateAsync(quizCategory);

                resultMessage = Constants.QUIZ_CATEGORY_DELETED_SUCCESS;
                break;

            case QuizCategoryActionType.ChangeStatus:
                if (quizCategoryAction.NewStatus is null)
                {
                    throw new AppException(Constants.STATUS_REQUIRED);
                }

                bool requestedStatusBool = quizCategoryAction.NewStatus != 0;

                if (quizCategory.Status == requestedStatusBool)
                {
                    string alreadyStatus = requestedStatusBool ? "active" : "inactive";
                    throw new AppException(string.Format(Constants.QUIZ_CATEGORY_STATUS_ALREADY_SET, alreadyStatus));
                }

                quizCategory.Status = requestedStatusBool;
                quizCategory.ModifiedBy = UserId;
                quizCategory.ModifiedDate = DateTime.UtcNow;

                await _quizCategoryRepository.UpdateAsync(quizCategory);

                resultMessage = requestedStatusBool
                    ? Constants.QUIZ_CATEGORY_ACTIVATED_SUCCESS
                    : Constants.QUIZ_CATEGORY_INACTIVATED_SUCCESS;
                break;


            default:
                throw new AppException(Constants.INVALID_DATA_MESSAGE);
        }

        await _quizCategoryRepository.UpdateAsync(quizCategory);

        if (resultMessage == Constants.QUIZ_CATEGORY_DELETED_SUCCESS || resultMessage == Constants.QUIZ_CATEGORY_ACTIVATED_SUCCESS || resultMessage == Constants.QUIZ_CATEGORY_INACTIVATED_SUCCESS)
        {
            dropDownDataService.ClearCache(DropDownType.QuizCategory);
        }

        return resultMessage;
    }
    #endregion

    #region Difficulty Name Available
    public async Task<bool> IsCategoryNameAvailable(string name, int? id = null)
    {
        bool exists = await _quizCategoryRepository.Exists(u => u.CategoryName.ToLower().Trim() == name.ToLower().Trim() && !u.IsDeleted && (id == null || u.Id != id.Value));

        if (exists)
            throw new AppException(Constants.DUPLICATE_QUIZ_CATEGORY);

        return true;
    }

    #endregion
}

