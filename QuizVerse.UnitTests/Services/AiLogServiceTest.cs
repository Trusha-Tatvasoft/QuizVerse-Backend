using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class AiLogServiceTest
{
    private readonly Mock<IGenericRepository<AiProcessLog>> _mockRepo;
    private readonly AiLogService _aiLogService;

    public AiLogServiceTest()
    {
        _mockRepo = new Mock<IGenericRepository<AiProcessLog>>();
        _aiLogService = new AiLogService(_mockRepo.Object);
    }

    [Fact]
    public void StartApiCall_ShouldCreateLog_WithExtraInfo()
    {
        // Arrange
        var startDetail = new AiApiCallStartDetail
        {
            ModelName = 1,
            Purpose = 2,
            ExtraInfo = "extra"
        };

        // Act
        var result = _aiLogService.StartApiCall(startDetail);

        // Assert
        Assert.Equal(startDetail.ModelName, result.ModelName);
        Assert.Equal(startDetail.Purpose, result.Purpose);
        Assert.Equal(startDetail.ExtraInfo, result.ExtraInfo);
        Assert.False(result.IsSuccess);
        Assert.True(result.StartTime <= DateTime.UtcNow);
    }

    [Fact]
    public void StartApiCall_ShouldCreateLog_WithoutExtraInfo()
    {
        // Arrange
        var startDetail = new AiApiCallStartDetail
        {
            ModelName = 5,
            Purpose = 9,
            ExtraInfo = null
        };

        // Act
        var result = _aiLogService.StartApiCall(startDetail);

        // Assert
        Assert.Equal(startDetail.ModelName, result.ModelName);
        Assert.Equal(startDetail.Purpose, result.Purpose);
        Assert.Null(result.ExtraInfo);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task EndApiCall_ShouldSetEndTimeAndSuccess_AndAddToRepository()
    {
        // Arrange
        var log = new AiProcessLog
        {
            ModelName = 1,
            Purpose = 2,
            IsSuccess = false
        };

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<AiProcessLog>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _aiLogService.EndApiCall(log, true);

        // Assert
        Assert.True(log.IsSuccess);
        Assert.True(log.EndTime <= DateTime.UtcNow);
        _mockRepo.Verify(r => r.AddAsync(It.Is<AiProcessLog>(x =>
            x.IsSuccess == true &&
            x.ModelName == 1 &&
            x.Purpose == 2
        )), Times.Once);
    }

    [Fact]
    public async Task EndApiCall_ShouldSetEndTimeAndFailure_AndAddToRepository()
    {
        // Arrange
        var log = new AiProcessLog
        {
            ModelName = 3,
            Purpose = 7,
            IsSuccess = true // will be changed
        };

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<AiProcessLog>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _aiLogService.EndApiCall(log, false);

        // Assert
        Assert.False(log.IsSuccess);
        Assert.True(log.EndTime <= DateTime.UtcNow);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<AiProcessLog>()), Times.Once);
    }
}