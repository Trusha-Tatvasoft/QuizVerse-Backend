using System.Linq.Dynamic.Core;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class QuizCategoryService(IGenericRepository<QuizCategory> _quizCategoryRepository, IMapper _mapper) : IQuizCategoryService
{
    public async Task<PageListResponse<QuizCategoryDTO>> GetQuizCategories(PageListRequest pageListRequest)
    {
        IQueryable<QuizCategory> quizCategories = _quizCategoryRepository.GetQueryableInclude(q => q.Quizzes);

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
            bool columnExists = typeof(QuizCategory).GetProperty(pageListRequest.SortColumn) != null;
            if (!columnExists)
            {
                throw new ArgumentException(
                    string.Format(Constants.INVALID_COLUMN_NAME, pageListRequest.SortColumn),
                    nameof(pageListRequest.SortColumn));
            }

            quizCategories = quizCategories.OrderBy($"{pageListRequest.SortColumn} {(pageListRequest.SortDescending ? "desc" : "asc")}");
        }
        else
        {
            quizCategories = quizCategories.OrderBy(q => q.Id);
        }

        // Paginate entities first
        PageListResponse<QuizCategory> pagedResult = await _quizCategoryRepository.PaginatedList<QuizCategory>(quizCategories, pageListRequest);

        // Map paginated entities to DTOs
        List<QuizCategoryDTO> quizCategoryDtos = _mapper.Map<List<QuizCategoryDTO>>(pagedResult.Records);

        // Set QuizCount for each category
        foreach (QuizCategoryDTO dto in quizCategoryDtos)
        {
            QuizCategory? matchingEntity = pagedResult.Records.FirstOrDefault(q => q.Id == dto.Id);
            dto.QuizCount = matchingEntity?.Quizzes?.Count ?? 0;
        }

        // Return paginated DTO response
        return new PageListResponse<QuizCategoryDTO>
        {
            Records = quizCategoryDtos,
            TotalRecords = totalRecords,
        };
    }
}
