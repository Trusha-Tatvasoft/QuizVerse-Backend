using Xunit;
using Moq;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Interface;
using Npgsql;

namespace QuizVerse.UnitTests.Services;

public class QuestionPoolServiceTest
{
    private readonly Mock<IGenericRepository<BaseQuestion>> _mockBaseQuestionRepo;
    private readonly Mock<ISqlQueryRepository> _mockSqlQueryRepo;
    private readonly QuestionPoolService _service;

    public QuestionPoolServiceTest()
    {
        _mockBaseQuestionRepo = new Mock<IGenericRepository<BaseQuestion>>();
        _mockSqlQueryRepo = new Mock<ISqlQueryRepository>();
        _service = new QuestionPoolService(_mockBaseQuestionRepo.Object, _mockSqlQueryRepo.Object);
    }

    [Fact]
    public async Task GetQuestionPoolListAsync_ReturnsPaginatedList()
    {
        // Arrange
        var request = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "sample",
            SortColumn = "queText",
            SortDescending = false,
            Filters = new FilterDto
            {
                QuizCategoryId = 1,
                QuestionDifficultyId = 2,
                QuestionTypeId = 3
            }
        };

        var questionPoolList = new List<QuestionPoolListDto>
        {
            new() { Id = 1, QueText = "Sample Question 1" },
            new() { Id = 2, QueText = "Sample Question 2" }
        };

        _mockSqlQueryRepo
            .Setup(repo => repo.SqlQueryListAsync<QuestionPoolListDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(questionPoolList);

        _mockBaseQuestionRepo
            .Setup(repo => repo.GetQueryableInclude())
            .Returns(new List<BaseQuestion> { new(), new(), new() }.AsQueryable());

        // Act
        var result = await _service.GetQuestionPoolListAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalRecords);
        Assert.Equal(2, result.Records.Count);
    }

    [Fact]
    public async Task GetQuestionPoolListAsync_WithEmptyResults_ReturnsEmptyList()
    {
        // Arrange
        var request = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = null,
            SortColumn = null,
            SortDescending = false,
            Filters = null
        };

        _mockSqlQueryRepo
            .Setup(repo => repo.SqlQueryListAsync<QuestionPoolListDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync([]);

        _mockBaseQuestionRepo
            .Setup(repo => repo.GetQueryableInclude())
            .Returns(new List<BaseQuestion>().AsQueryable());

        // Act
        var result = await _service.GetQuestionPoolListAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalRecords);
        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task GetQuestionPoolListAsync_NullFilters_DoesNotThrow()
    {
        // Arrange
        var request = new PageListRequest
        {
            PageNumber = 1,
            PageSize = 5,
            Filters = null
        };

        _mockSqlQueryRepo
            .Setup(repo => repo.SqlQueryListAsync<QuestionPoolListDto>(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync([]);

        _mockBaseQuestionRepo
            .Setup(repo => repo.GetQueryableInclude())
            .Returns(new List<BaseQuestion>().AsQueryable());

        // Act
        var result = await _service.GetQuestionPoolListAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Records);
    }
}
