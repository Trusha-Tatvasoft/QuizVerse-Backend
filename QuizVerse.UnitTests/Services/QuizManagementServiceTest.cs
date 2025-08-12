using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Data;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Mappings;
using QuizVerse.Infrastructure.Repository;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class QuizManagementServiceTests
{
    private readonly QuizVerseDbContext _context;
    private readonly QuizManagementService _quizService;
    private readonly IMapper _mapper;

    public QuizManagementServiceTests()
    {
        var options = new DbContextOptionsBuilder<QuizVerseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new QuizVerseDbContext(options);
        SeedTestData();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();

        var quizRepo = new GenericRepository<Quiz>(_context);
        _quizService = new QuizManagementService(quizRepo, _mapper);
    }

    private void SeedTestData()
    {
        var category = new QuizCategory
        {
            Id = 1,
            CategoryName = "General Knowledge",
            Description = "Science related quizzes",
            Status = true,
            IsDeleted = false,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        var difficulty = new QuizDifficulty
        {
            Id = 1,
            Name = "Easy",
            Description = "Easy level questions",
            IsDeleted = false,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        if (!_context.QuizCategories.Any())
            _context.QuizCategories.Add(category);

        if (!_context.QuizDifficulties.Any())
            _context.QuizDifficulties.Add(difficulty);

        _context.SaveChanges();

        var quiz1 = new Quiz
        {
            Id = 1,
            Name = "Quiz 1",
            Description = "This is quiz 1",
            Status = (int)QuizStatus.Active,
            CategoryId = category.Id,
            DifficultyLevelId = difficulty.Id,
            IsDeleted = false
        };

        var quiz2 = new Quiz
        {
            Id = 2,
            Name = "Quiz 2",
            Description = "This is quiz 2",
            Status = (int)QuizStatus.Inactive,
            CategoryId = category.Id,
            DifficultyLevelId = difficulty.Id,
            IsDeleted = false
        };

        _context.Quizzes.AddRange(quiz1, quiz2);
        _context.SaveChanges();

        // Add attempt data after quizzes exist
        var quizAttempteds = new List<QuizAttempted>
    {
        new QuizAttempted { QuizId = quiz1.Id, UserId = 1, CreatedDate = DateTime.Now },
        new QuizAttempted { QuizId = quiz1.Id, UserId = 2, CreatedDate = DateTime.Now },
        new QuizAttempted { QuizId = quiz2.Id, UserId = 1, CreatedDate = DateTime.Now }
    };

        var questionMaps = new List<QuizToBaseQuestionMap>
    {
        new QuizToBaseQuestionMap { QuizId = quiz1.Id, QueId = 1 },
        new QuizToBaseQuestionMap { QuizId = quiz1.Id, QueId = 2 },
        new QuizToBaseQuestionMap { QuizId = quiz2.Id, QueId = 2 },
        new QuizToBaseQuestionMap { QuizId = quiz2.Id, QueId = 3 }
    };

        _context.QuizAttempteds.AddRange(quizAttempteds);
        _context.QuizToBaseQuestionMaps.AddRange(questionMaps);
        _context.SaveChanges();
    }

    // #region Quiz Card Data
    // [Fact]
    // public async Task GetQuizCardData_ReturnsCorrectStats()
    // {
    //     var result = await _quizService.GetQuizCardData();

    //     Assert.Equal(2, result.TotalQuiz);
    //     Assert.Equal(1, result.ActiveQuiz);
    //     Assert.Equal(2, result.TotalParticipants);
    //     Assert.Equal(3, result.TotalQuestions);
    // }
    // #endregion
    // #region Quiz Management List
    // [Fact]
    // public async Task GetQuizzesByPagination_WithSearchTerm_ReturnsFilteredResults()
    // {
    //     var request = new PageListRequest
    //     {
    //         PageNumber = 1,
    //         PageSize = 10,
    //         SearchTerm = "Quiz 1"
    //     };

    //     var result = await _quizService.GetQuizzesByPagination(request);

    //     Assert.Single(result.Records);
    //     Assert.Equal("Quiz 1", result.Records.First().QuizTitle);
    // }

    // [Fact]
    // public async Task GetQuizzesByPagination_WithInvalidStatus_ThrowsAppException()
    // {
    //     var request = new PageListRequest
    //     {
    //         Filters = new FilterDto { QuizStatus = (QuizStatus)999 }
    //     };

    //     var ex = await Assert.ThrowsAsync<AppException>(() => _quizService.GetQuizzesByPagination(request));
    //     Assert.Equal(Constants.INVALID_QUIZ_STATUS_MESSAGE, ex.Message);
    // }

    // [Fact]
    // public async Task GetQuizzesByPagination_WithCategoryFilter_WorksCorrectly()
    // {
    //     var request = new PageListRequest
    //     {
    //         Filters = new FilterDto { QuizCategoryId = 1 }
    //     };

    //     var result = await _quizService.GetQuizzesByPagination(request);

    //     Assert.Equal(2, result.Records.Count());
    // }

    // [Fact]
    // public async Task GetQuizzesByPagination_WithDifficultyFilter_WorksCorrectly()
    // {
    //     var request = new PageListRequest
    //     {
    //         Filters = new FilterDto { QuizDifficultyId = 1 }
    //     };

    //     var result = await _quizService.GetQuizzesByPagination(request);

    //     Assert.Equal(2, result.Records.Count());
    // }

    // [Fact]
    // public async Task GetQuizzesByPagination_SortingDescending_WorksCorrectly()
    // {
    //     var request = new PageListRequest
    //     {
    //         SortColumn = "name",
    //         SortDescending = true
    //     };

    //     var result = await _quizService.GetQuizzesByPagination(request);

    //     Assert.Equal(2, result.Records.Count());
    //     Assert.Equal("Quiz 2", result.Records.First().QuizTitle);
    // }

    // [Fact]
    // public async Task GetQuizzesByPagination_WithValidStatusFilter_ReturnsCorrectResults()
    // {
    //     var request = new PageListRequest
    //     {
    //         Filters = new FilterDto { QuizStatus = QuizStatus.Active }
    //     };

    //     var result = await _quizService.GetQuizzesByPagination(request);

    //     Assert.Single(result.Records);
    //     Assert.Equal((int)QuizStatus.Active, result.Records.First().Status);
    // }

    // [Fact]
    // public async Task GetQuizzesByPagination_SortByCategory_WorksCorrectly()
    // {
    //     var request = new PageListRequest
    //     {
    //         SortColumn = "category",
    //         SortDescending = false
    //     };

    //     var result = await _quizService.GetQuizzesByPagination(request);

    //     Assert.Equal(2, result.Records.Count());
    //     Assert.All(result.Records, r => Assert.Equal("General Knowledge", r.CategoryName));
    // }

    // [Fact]
    // public async Task GetQuizzesByPagination_SortByDifficulty_WorksCorrectly()
    // {
    //     var request = new PageListRequest
    //     {
    //         SortColumn = "difficulty",
    //         SortDescending = false
    //     };

    //     var result = await _quizService.GetQuizzesByPagination(request);

    //     Assert.Equal(2, result.Records.Count());
    //     Assert.All(result.Records, r => Assert.Equal("Easy", r.QuizDifficultyLevel));
    // }
    // #endregion
}
