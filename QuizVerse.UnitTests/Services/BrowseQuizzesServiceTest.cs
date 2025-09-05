using Moq;
using Xunit;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.UnitTests.Services;

public class BrowseQuizzesServiceTests
{
    private readonly Mock<ISqlQueryRepository> _sqlQueryRepoMock;
    private readonly BrowseQuizzesService _service;

    public BrowseQuizzesServiceTests()
    {
        _sqlQueryRepoMock = new Mock<ISqlQueryRepository>();
        _service = new BrowseQuizzesService(_sqlQueryRepoMock.Object);
    }

    [Fact]
    public async Task BrowseQuizzes_Sets_Defaults_When_Null()
    {
        // Arrange
        var request = new BrowseQuizzesRequestDTO
        {
            BrowseQuizzesSorting = null,
            SearchText = null,
            BatchNumber = 1
        };

        _sqlQueryRepoMock
            .Setup(r => r.SqlQuerySingleAsync<BrowseQuizzesResultDTO>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(new BrowseQuizzesResultDTO { QuizzesJSON = "[]", HasMore = false });

        // Act
        var result = await _service.BrowseQuizzes(request);

        // Assert
        Assert.Equal(BrowseQuizzesSorting.Newest, request.BrowseQuizzesSorting);
        Assert.Equal("", request.SearchText);
        Assert.NotNull(result.Quizzes);
        Assert.False(result.HasMore);
    }

    [Fact]
    public async Task BrowseQuizzes_Throws_When_MinPrice_Greater_Than_MaxPrice()
    {
        var request = new BrowseQuizzesRequestDTO
        {
            BatchNumber = 1,
            FilterRanges = new FilterRanges { MinPrice = 10, MaxPrice = 5 }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.BrowseQuizzes(request));
    }

    [Fact]
    public async Task BrowseQuizzes_Throws_When_MinRating_Greater_Than_MaxRating()
    {
        var request = new BrowseQuizzesRequestDTO
        {
            BatchNumber = 1,
            FilterRanges = new FilterRanges { MinRating = 5, MaxRating = 2 }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.BrowseQuizzes(request));
    }

    [Fact]
    public async Task BrowseQuizzes_Throws_When_MinTotalTime_Greater_Than_MaxTotalTime()
    {
        var request = new BrowseQuizzesRequestDTO
        {
            BatchNumber = 1,
            FilterRanges = new FilterRanges { MinTotalTime = 100, MaxTotalTime = 50 }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.BrowseQuizzes(request));
    }

    [Fact]
    public async Task BrowseQuizzes_Returns_Quizzes_When_Json_Is_Valid()
    {
        var quizzesJson = "[{\"id\":1,\"name\":\"Test Quiz\",\"description\":\"Desc\",\"is_paid\":true," +
                          "\"price\":9.99,\"category_name\":\"General\",\"difficulty_level\":\"Easy\"," +
                          "\"is_featured\":true,\"tags\":[\"tag1\"],\"total_time\":60," +
                          "\"total_questions\":10,\"total_participates\":100,\"rating\":4.5}]";

        _sqlQueryRepoMock
            .Setup(r => r.SqlQuerySingleAsync<BrowseQuizzesResultDTO>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(new BrowseQuizzesResultDTO { QuizzesJSON = quizzesJson, HasMore = true });

        var request = new BrowseQuizzesRequestDTO { BatchNumber = 1 };

        // Act
        var result = await _service.BrowseQuizzes(request);

        // Assert
        Assert.Single(result.Quizzes);
        Assert.True(result.HasMore);
        Assert.Equal("Test Quiz", result.Quizzes[0].Name);
    }

    [Fact]
    public async Task BrowseQuizzes_Returns_Empty_List_When_Json_Is_Null_Or_Whitespace()
    {
        _sqlQueryRepoMock
            .Setup(r => r.SqlQuerySingleAsync<BrowseQuizzesResultDTO>(It.IsAny<string>(), It.IsAny<object[]>()))
            .ReturnsAsync(new BrowseQuizzesResultDTO { QuizzesJSON = " ", HasMore = false });

        var request = new BrowseQuizzesRequestDTO { BatchNumber = 1 };

        var result = await _service.BrowseQuizzes(request);

        Assert.Empty(result.Quizzes);
    }
}
