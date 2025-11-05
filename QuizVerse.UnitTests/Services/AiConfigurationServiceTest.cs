using System.Text.Json;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.Infrastructure.Common;
using Xunit;
using System.Linq.Expressions;

namespace QuizVerse.UnitTests.Services;
public class AiConfigurationServiceTests
{
    private readonly Mock<IGenericRepository<AiProcessLog>> _mockRepo;
    private readonly AiConfigurationService _service;

    public AiConfigurationServiceTests()
    {
        _mockRepo = new Mock<IGenericRepository<AiProcessLog>>();
        _service = new AiConfigurationService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetAiConfigurationCardDetails_ShouldCalculate_AllValuesCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var currentMonthLogs = new List<AiProcessLog>
            {
                new AiProcessLog
                {
                    Purpose = (int)AiApiPurpose.QuestionGeneration,
                    StartTime = now,
                    ExtraInfo = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        { Constants.GENERATED_QUESTIONS_COUNT_JSON_KEY, 5 }
                    })
                },
                new AiProcessLog
                {
                    Purpose = (int)AiApiPurpose.QuestionGeneration,
                    StartTime = now,
                    ExtraInfo = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        { Constants.GENERATED_QUESTIONS_COUNT_JSON_KEY, 3 }
                    })
                },
                new AiProcessLog
                {
                    Purpose = (int)AiApiPurpose.QuestionGeneration,
                    StartTime = now,
                    ExtraInfo = "invalid json"
                }
            };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AiProcessLog, bool>>>()))
            .ReturnsAsync(currentMonthLogs);

        // for month counts (current + last)
        _mockRepo.SetupSequence(r => r.CountAsync(It.IsAny<Expression<Func<AiProcessLog, bool>>>()))
            .ReturnsAsync(10)
            .ReturnsAsync(10)
            .ReturnsAsync(15);

        _mockRepo.Setup(r => r.CountAsync((Expression<Func<AiProcessLog, bool>>?)null))
            .ReturnsAsync(20); // total api calls

        // Act
        var result = await _service.GetAiConfigurationCardDetails();

        // Assert
        Assert.Equal(10, result.CurruntMonthApiCalls);
        Assert.Equal(8, result.GeneratedQuestionsCurruntMonth);
        Assert.Equal(10, result.GeneratedQuestionsLastMonth);
        Assert.Equal(75.00m, result.SuccessRate);
    }

    [Fact]
    public async Task GetAiConfigurationCardDetails_ShouldHandle_NoLogsGracefully()
    {
        // Arrange
        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AiProcessLog, bool>>>()))
            .ReturnsAsync(new List<AiProcessLog>());

        _mockRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<AiProcessLog, bool>>>()))
            .ReturnsAsync(0);

        _mockRepo.Setup(r => r.CountAsync((Expression<Func<AiProcessLog, bool>>?)null))
            .ReturnsAsync(0);

        // Act
        var result = await _service.GetAiConfigurationCardDetails();

        // Assert
        Assert.Equal(0, result.CurruntMonthApiCalls);
        Assert.Equal(0, result.GeneratedQuestionsCurruntMonth);
        Assert.Equal(0, result.GeneratedQuestionsLastMonth);
        Assert.Equal(0, result.SuccessRate);
    }

    [Fact]
    public async Task GetAiUsesDetails_ShouldCalculate_AllMetricsCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var logs = new List<AiProcessLog>
            {
                new AiProcessLog
                {
                    StartTime = now.AddMinutes(-2),
                    EndTime = now,
                    IsSuccess = true,
                    ModelName = (int)AiModelName.Gemini2Point0Flash
                },
                new AiProcessLog
                {
                    StartTime = now.AddMinutes(-4),
                    EndTime = now.AddMinutes(-1),
                    IsSuccess = false,
                    ModelName = (int)AiModelName.Gemini2Point0Flash
                }
            };

        _mockRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<AiProcessLog, bool>>>()))
            .ReturnsAsync(2); // today's calls

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AiProcessLog, bool>>>()))
            .ReturnsAsync(logs);

        // Act
        var result = await _service.GetAiUsesDetails(AiModelName.Gemini2Point0Flash);

        // Assert
        Assert.Equal(2, result.TodaysApiCalls);
        Assert.True(result.AverageResponseTimeInSecond > 0);
        Assert.Equal(50.00m, result.ErrorRate); // 1 out of 2 failed
    }

    [Fact]
    public async Task GetAiUsesDetails_ShouldHandle_NoLogs_ReturnZeroMetrics()
    {
        // Arrange
        _mockRepo.Setup(r => r.CountAsync(It.IsAny<Expression<Func<AiProcessLog, bool>>>()))
            .ReturnsAsync(0);

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AiProcessLog, bool>>>()))
            .ReturnsAsync(new List<AiProcessLog>());

        // Act
        var result = await _service.GetAiUsesDetails(null);

        // Assert
        Assert.Equal(0, result.TodaysApiCalls);
        Assert.Equal(0, result.AverageResponseTimeInSecond);
        Assert.Equal(0, result.ErrorRate);
    }
}