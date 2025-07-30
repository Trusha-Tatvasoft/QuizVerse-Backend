using System.ComponentModel.DataAnnotations;
using QuizVerse.Infrastructure.Common;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class PageListRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = SystemConstants.DEFAULT_PAGE_SIZE;

    public string? SearchTerm { get; set; }
    public string? SortColumn { get; set; }
    public bool SortDescending { get; set; } = true;
    public FilterDto? Filters { get; set; }
}
