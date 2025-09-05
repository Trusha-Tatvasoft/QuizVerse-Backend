using QuizVerse.Infrastructure.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuizVerse.Infrastructure.DTOs.RequestDTOs;

public class BrowseQuizzesRequestDTO
{
    public string? SearchText { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "QuizCategoryId must be greater than 0")]
    public int? QuizCategoryId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "QuizDifficultyLevelId must be greater than 0")]
    public int? QuizDifficultyLevelId { get; set; }

    public int[]? TagIds { get; set; }

    public BrowseQuizzesSorting? BrowseQuizzesSorting { get; set; }

    public BrowseQuizzesFilterByType? BrowseQuizzesFilterByType { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "BatchNumber must be greater than 0")]
    public int BatchNumber { get; set; }

    public FilterRanges? FilterRanges { get; set; }
}

public class FilterRanges
{
    [Range(0, double.MaxValue, ErrorMessage = "MinPrice must be >= 0")]
    public decimal? MinPrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "MaxPrice must be >= 0")]
    public decimal? MaxPrice { get; set; }

    [Range(0, 5, ErrorMessage = "Min Rating must be between 0 and 5")]
    public decimal? MinRating { get; set; }

    [Range(0, 5, ErrorMessage = "Max Rating must be between 0 and 5")]
    public decimal? MaxRating { get; set; }

    [Range(2, 180, ErrorMessage = "Min Total Time must be between 2 and 180 minutes")]
    public decimal? MinTotalTime { get; set; }

    [Range(2, 180, ErrorMessage = "Max Total Time must be between 2 and 180 minutes")]
    public decimal? MaxTotalTime { get; set; }
}

public class BrowseQuizzesResultDTO
{
    [Column("quizzes")]
    public string QuizzesJSON { get; set; } = string.Empty;

    [Column("has_more")]
    public bool HasMore { get; set; }
}





