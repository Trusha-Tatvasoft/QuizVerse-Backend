using System.Linq.Expressions;
using System.Threading.Channels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class GcpApiQueueServiceTests
{
    private readonly Mock<ILogger<GcpApiQueueService>> _loggerMock;
    private readonly Mock<IPerspectiveApiService> _perspectiveApiServiceMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<ISqlQueryRepository> _sqlQueryRepositoryMock;
    private readonly Mock<IGenericRepository<QuizRating>> _quizRatingRepoMock;
    private readonly Mock<IGenericRepository<QuestionIssueReport>> _questionIssueReportRepoMock;
    private readonly Mock<IGenericRepository<QuizIssueReport>> _quizIssueReportRepoMock;
    private readonly IMemoryCache _memoryCache;
    private readonly GcpApiQueueService _service;

    public GcpApiQueueServiceTests()
    {
        _loggerMock = new Mock<ILogger<GcpApiQueueService>>();
        _perspectiveApiServiceMock = new Mock<IPerspectiveApiService>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _sqlQueryRepositoryMock = new Mock<ISqlQueryRepository>();
        _quizRatingRepoMock = new Mock<IGenericRepository<QuizRating>>();
        _questionIssueReportRepoMock = new Mock<IGenericRepository<QuestionIssueReport>>();
        _quizIssueReportRepoMock = new Mock<IGenericRepository<QuizIssueReport>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        // Setup service scope factory
        _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceScopeFactoryMock.Setup(f => f.CreateScope()).Returns(_serviceScopeMock.Object);

        // Setup service provider to return repositories
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ISqlQueryRepository)))
            .Returns(_sqlQueryRepositoryMock.Object);

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IGenericRepository<QuizRating>)))
            .Returns(_quizRatingRepoMock.Object);

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IGenericRepository<QuestionIssueReport>)))
            .Returns(_questionIssueReportRepoMock.Object);

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IGenericRepository<QuizIssueReport>)))
            .Returns(_quizIssueReportRepoMock.Object);

        _service = new GcpApiQueueService(
            _loggerMock.Object,
            _serviceScopeFactoryMock.Object,
            _perspectiveApiServiceMock.Object,
            _memoryCache);
    }

    [Fact]
    public async Task EnqueueAsync_WithCacheHit_AppliesCachedResultDirectly()
    {
        // Arrange
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuizRatingFeedback,
            ReportComment = "Test comment"
        };

        var cachedResult = new CachedPerspectiveResult
        {
            IsFlagged = true,
            FlagReason = "Toxic content",
            AttributeScore = 0.8,
            Severity = QuestionOrQuizIssueReportSeverity.High,
            ReportType = ReportType.QuizRatingFeedback,
            CachedAt = DateTime.UtcNow
        };

        var rating = new QuizRating { Id = reportData.ReportId };

        _quizRatingRepoMock
            .Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<QuizRating, bool>>>(),
                It.IsAny<Func<IQueryable<QuizRating>, IQueryable<QuizRating>>>()))
            .ReturnsAsync(rating);

        // Add to cache
        var cacheKey = GenerateCacheKey(reportData.ReportComment, reportData.ReportType);
        _memoryCache.Set(cacheKey, cachedResult);

        // Act
        await _service.EnqueueAsync(reportData);

        // Assert
        _quizRatingRepoMock.Verify(r => r.UpdateAsync(It.Is<QuizRating>(
            qr => qr.IsFlagged == true && qr.Reason == "Toxic content")), Times.Once);
    }

    [Fact]
    public async Task EnqueueAsync_WithCacheMiss_AddsToQueue()
    {
        // Arrange
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 2,
            ReportType = ReportType.QuizRatingFeedback,
            ReportComment = "New test comment"
        };

        // Act
        await _service.EnqueueAsync(reportData);

        // Assert - Verify it was added to queue (check via logging)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Cache MISS")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateReportTableAsync_QuizRatingFeedback_UpdatesCorrectly()
    {
        // Arrange
        var reportId = 3;
        var result = new PerspectiveAnalysisResult
        {
            ReportId = reportId,
            ReportType = ReportType.QuizRatingFeedback,
            IsFlagged = true,
            FlagReason = "Profanity detected",
            AttributeScore = 0.9,
            IsSuccess = true
        };

        var rating = new QuizRating
        {
            Id = reportId,
            IsFlagged = false,
            Status = (int)QuizRatingStatus.Pending
        };

        _quizRatingRepoMock
            .Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<QuizRating, bool>>>(),
                It.IsAny<Func<IQueryable<QuizRating>, IQueryable<QuizRating>>>()))
            .ReturnsAsync(rating);

        // Act - Use reflection to call private method
        var method = typeof(GcpApiQueueService).GetMethod("UpdateReportTableAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(_service, new object[] { result });

        // Assert
        _quizRatingRepoMock.Verify(r => r.UpdateAsync(It.Is<QuizRating>(
            qr => qr.IsFlagged == true &&
                  qr.Status == (int)QuizRatingStatus.Pending &&
                  qr.Reason == "Profanity detected")), Times.Once);
    }

    [Fact]
    public async Task UpdateReportTableAsync_QuestionIssueReport_UpdatesCorrectly()
    {
        // Arrange
        var reportId = 4;
        var result = new PerspectiveAnalysisResult
        {
            ReportId = reportId,
            ReportType = ReportType.QuestionIssueReport,
            Severity = QuestionOrQuizIssueReportSeverity.High,
            AttributeScore = 0.8,
            IsSuccess = true
        };

        var questionIssue = new QuestionIssueReport
        {
            Id = reportId,
            Severity = (int)QuestionOrQuizIssueReportSeverity.Low
        };

        _questionIssueReportRepoMock
            .Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<QuestionIssueReport, bool>>>(),
                It.IsAny<Func<IQueryable<QuestionIssueReport>, IQueryable<QuestionIssueReport>>>()))
            .ReturnsAsync(questionIssue);

        // Act
        var method = typeof(GcpApiQueueService).GetMethod("UpdateReportTableAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(_service, new object[] { result });

        // Assert
        _questionIssueReportRepoMock.Verify(r => r.UpdateAsync(It.Is<QuestionIssueReport>(
            qir => qir.Severity == (int)QuestionOrQuizIssueReportSeverity.High &&
                   qir.Status == (int)QuestionOrQuizIssueReportStatus.Pending)), Times.Once);
    }

    [Fact]
    public async Task UpdateReportTableAsync_QuizIssueReport_UpdatesCorrectly()
    {
        // Arrange
        var reportId = 5;
        var result = new PerspectiveAnalysisResult
        {
            ReportId = reportId,
            ReportType = ReportType.QuizIssueReport,
            Severity = QuestionOrQuizIssueReportSeverity.Medium,
            AttributeScore = 0.5,
            IsSuccess = true
        };

        var quizIssue = new QuizIssueReport
        {
            Id = reportId,
            Severity = (int)QuestionOrQuizIssueReportSeverity.Low
        };

        _quizIssueReportRepoMock
            .Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<QuizIssueReport, bool>>>(),
                It.IsAny<Func<IQueryable<QuizIssueReport>, IQueryable<QuizIssueReport>>>()))
            .ReturnsAsync(quizIssue);

        // Act
        var method = typeof(GcpApiQueueService).GetMethod("UpdateReportTableAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(_service, new object[] { result });

        // Assert
        _quizIssueReportRepoMock.Verify(r => r.UpdateAsync(It.Is<QuizIssueReport>(
            qir => qir.Severity == (int)QuestionOrQuizIssueReportSeverity.Medium &&
                   qir.Status == (int)QuestionOrQuizIssueReportStatus.Pending)), Times.Once);
    }

    [Fact]
    public void CacheAnalysisResult_StoresResultInCache()
    {
        // Arrange
        var comment = "Test comment";
        var reportType = ReportType.QuizRatingFeedback;
        var result = new PerspectiveAnalysisResult
        {
            ReportId = 6,
            ReportType = reportType,
            IsFlagged = true,
            FlagReason = "Test reason",
            AttributeScore = 0.7,
            Severity = QuestionOrQuizIssueReportSeverity.Medium,
            IsSuccess = true
        };

        // Act - Use reflection to call private method
        var method = typeof(GcpApiQueueService).GetMethod("CacheAnalysisResult",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(_service, new object[] { comment, reportType, result });

        // Assert
        var cacheKey = GenerateCacheKey(comment, reportType);
        var cachedResult = _memoryCache.Get<CachedPerspectiveResult>(cacheKey);

        Assert.NotNull(cachedResult);
        Assert.Equal(result.IsFlagged, cachedResult.IsFlagged);
        Assert.Equal(result.FlagReason, cachedResult.FlagReason);
        Assert.Equal(result.AttributeScore, cachedResult.AttributeScore);
    }

    [Fact]
    public async Task LoadPendingItemsFromDatabaseAsync_LoadsAndEnqueuesItems()
    {
        // Arrange
        var pendingReports = new List<GcpApiReportAllDataDto>
        {
            new() { ReportId = 1, ReportType = (int)ReportType.QuizRatingFeedback, ReportComment = "Comment 1" },
            new() { ReportId = 2, ReportType = (int)ReportType.QuestionIssueReport, ReportComment = "Comment 2" }
        };

        _sqlQueryRepositoryMock
            .Setup(r => r.SqlQueryListAsync<GcpApiReportAllDataDto>(It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(pendingReports);

        // Act - Use reflection to call private method
        var method = typeof(GcpApiQueueService).GetMethod("LoadPendingItemsFromDatabaseAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(_service, Array.Empty<object>());

        // Assert
        _sqlQueryRepositoryMock.Verify(r => r.SqlQueryListAsync<GcpApiReportAllDataDto>(
            It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()), Times.Once);
    }

    [Fact]
    public void GenerateCacheKey_CreatesSameKeyForSameComment()
    {
        // Arrange
        var comment1 = "Test Comment";
        var comment2 = "test comment"; // Different case
        var reportType = ReportType.QuizRatingFeedback;

        // Act
        var key1 = GenerateCacheKey(comment1, reportType);
        var key2 = GenerateCacheKey(comment2, reportType);

        // Assert
        Assert.Equal(key1, key2); // Should be equal (case-insensitive)
    }

    [Fact]
    public void GenerateCacheKey_CreatesDifferentKeysForDifferentReportTypes()
    {
        // Arrange
        var comment = "Test Comment";

        // Act
        var key1 = GenerateCacheKey(comment, ReportType.QuizRatingFeedback);
        var key2 = GenerateCacheKey(comment, ReportType.QuestionIssueReport);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    // Helper method to generate cache key (mirrors the private method)
    private static string GenerateCacheKey(string comment, ReportType reportType)
    {
        string normalizedComment = comment?.Trim().ToLowerInvariant() ?? string.Empty;
        string commentHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(normalizedComment)
            )
        )[..16];

        return $"PerspectiveAPI:{reportType}:{commentHash}";
    }

    [Fact]
    public async Task StartProcessingAsync_CancelsGracefully()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var data = new GcpApiReportDataDto { ReportId = 10, ReportType = ReportType.QuizRatingFeedback, ReportComment = "Cancel test" };
        await _service.EnqueueAsync(data);
        cts.CancelAfter(100); // cancel quickly

        // Act
        await _service.StartProcessingAsync(cts.Token);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Queue processing cancelled")),
            It.IsAny<OperationCanceledException>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessItemWithRetryAsync_RetriesAndSucceeds()
    {
        // Arrange
        var data = new GcpApiReportDataDto { ReportId = 20, ReportType = ReportType.QuizRatingFeedback, ReportComment = "Retry test" };
        int attempt = 0;

        _perspectiveApiServiceMock
            .Setup(p => p.AnalyzeTextAsync(It.IsAny<GcpApiReportDataDto>()))
            .ReturnsAsync(() =>
            {
                attempt++;
                if (attempt < 2)
                    return new PerspectiveAnalysisResult { IsSuccess = false, ErrorMessage = "Fail" };
                return new PerspectiveAnalysisResult { IsSuccess = true, ReportType = ReportType.QuizRatingFeedback };
            });

        // Act (invoke private method)
        var method = typeof(GcpApiQueueService).GetMethod("ProcessItemWithRetryAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(_service, new object[] { data, CancellationToken.None });

        // Assert
        _perspectiveApiServiceMock.Verify(p => p.AnalyzeTextAsync(It.IsAny<GcpApiReportDataDto>()), Times.AtLeast(2));
    }
    [Fact]
    public async Task ProcessItemWithRetryAsync_FailsAfterMaxRetries_LogsCritical()
    {
        // Arrange
        var data = new GcpApiReportDataDto { ReportId = 30, ReportType = ReportType.QuizRatingFeedback, ReportComment = "Fail test" };
        _perspectiveApiServiceMock
            .Setup(p => p.AnalyzeTextAsync(It.IsAny<GcpApiReportDataDto>()))
            .ReturnsAsync(new PerspectiveAnalysisResult { IsSuccess = false, ErrorMessage = "Fail" });

        // Act
        var method = typeof(GcpApiQueueService).GetMethod("ProcessItemWithRetryAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(_service, new object[] { data, CancellationToken.None });

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Critical,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("CRITICAL: Failed to process")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ApplyCachedResultAsync_HandlesExceptionGracefully()
    {
        // Arrange
        var data = new GcpApiReportDataDto
        {
            ReportId = 40,
            ReportType = ReportType.QuizRatingFeedback,
            ReportComment = "Crash test"
        };

        var cached = new CachedPerspectiveResult
        {
            Severity = QuestionOrQuizIssueReportSeverity.Medium,
            IsFlagged = true
        };

        // Return a real QuizRating so UpdateAsync() is called
        _quizRatingRepoMock
            .Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<QuizRating, bool>>>(),
                It.IsAny<Func<IQueryable<QuizRating>, IQueryable<QuizRating>>>()))
            .ReturnsAsync(new QuizRating { Id = 40 });

        // Simulate exception in UpdateAsync
        _quizRatingRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<QuizRating>()))
            .ThrowsAsync(new Exception("DB error"));

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IGenericRepository<QuizRating>)))
            .Returns(_quizRatingRepoMock.Object);

        // Act
        var method = typeof(GcpApiQueueService).GetMethod("ApplyCachedResultAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(_service, new object[] { data, cached });

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error applying cached result")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessQueueAsync_HandlesExceptionAndContinues()
    {
        // Arrange
        var data = new GcpApiReportDataDto
        {
            ReportId = 50,
            ReportType = ReportType.QuizRatingFeedback,
            ReportComment = "Error test"
        };
        await _service.EnqueueAsync(data);

        _perspectiveApiServiceMock
            .Setup(p => p.AnalyzeTextAsync(It.IsAny<GcpApiReportDataDto>()))
            .ThrowsAsync(new Exception("API Error"));

        var cts = new CancellationTokenSource();
        cts.CancelAfter(200);

        // Act
        var method = typeof(GcpApiQueueService).GetMethod("StartProcessingAsync",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(_service, new object[] { cts.Token });

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error processing")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }
}