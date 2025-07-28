using System.Text.Json;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using Xunit;
using System.Linq.Expressions;

namespace QuizVerse.UnitTests.Services;

public class LandingPageServiceTests
{
    private readonly Mock<IGenericRepository<User>> _userRepoMock = new();
    private readonly Mock<IGenericRepository<Quiz>> _quizRepoMock = new();
    private readonly Mock<IGenericRepository<BaseQuestion>> _questionRepoMock = new();
    private readonly Mock<IGenericRepository<PlatformConfiguration>> _platformConfigRepoMock = new();

    private LandingPageService CreateService() =>
        new LandingPageService(
            _userRepoMock.Object,
            _quizRepoMock.Object,
            _questionRepoMock.Object,
            _platformConfigRepoMock.Object
        );

    [Fact]
    public async Task GetLandingPageDataAsync_ReturnsCorrectData_WithQuoteFromConfig()
    {
        _userRepoMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<User, bool>>>()))
                     .ReturnsAsync(100);
        _quizRepoMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Quiz, bool>>>()))
                     .ReturnsAsync(200);
        _questionRepoMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>()))
                         .ReturnsAsync(300);

        var configJson = JsonSerializer.Serialize(new { PlatformQuote = "Test Quote from Config" });
        _platformConfigRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PlatformConfiguration, bool>>>(), null))
                                .ReturnsAsync(new PlatformConfiguration { Values = configJson });


        var service = CreateService();

        LandingPageData result = await service.GetLandingPageData();

        Assert.Equal("Test Quote from Config", result.Quote);
        Assert.Equal(100, result.ActivePlayer);
        Assert.Equal(200, result.QuizCreated);
        Assert.Equal(300, result.QuestionAns);
    }

    [Fact]
    public async Task GetLandingPageDataAsync_UsesDefaultQuote_WhenConfigIsMissing()
    {
        _userRepoMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<User, bool>>>()))
                     .ReturnsAsync(1);
        _quizRepoMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Quiz, bool>>>()))
                     .ReturnsAsync(2);
        _questionRepoMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>()))
                         .ReturnsAsync(3);

        _platformConfigRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PlatformConfiguration, bool>>>(), null))
                               .ReturnsAsync((PlatformConfiguration?)null);

        var service = CreateService();

        var result = await service.GetLandingPageData();

        Assert.Equal("Welcome to QuizVerse!", result.Quote);
        Assert.Equal(1, result.ActivePlayer);
        Assert.Equal(2, result.QuizCreated);
        Assert.Equal(3, result.QuestionAns);
    }

    [Fact]
    public async Task GetLandingPageDataAsync_UsesDefaultQuote_WhenConfigValueIsInvalidJson()
    {
        _userRepoMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<User, bool>>>()))
                     .ReturnsAsync(10);
        _quizRepoMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Quiz, bool>>>()))
                     .ReturnsAsync(20);
        _questionRepoMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<BaseQuestion, bool>>>()))
                         .ReturnsAsync(30);

        _platformConfigRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<PlatformConfiguration, bool>>>(), null))
                               .ReturnsAsync(new PlatformConfiguration { Values = "invalid json" });

        var service = CreateService();

        var result = await service.GetLandingPageData();

        Assert.Equal("Welcome to QuizVerse!", result.Quote);
        Assert.Equal(10, result.ActivePlayer);
        Assert.Equal(20, result.QuizCreated);
        Assert.Equal(30, result.QuestionAns);
    }
}
