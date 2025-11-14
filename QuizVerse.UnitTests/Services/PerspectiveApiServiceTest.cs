using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class PerspectiveApiServiceTest
{
    private readonly Mock<ILogger<PerspectiveApiService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly PerspectiveApiService _service;
    private readonly Mock<IAiLogService> _aiLogService;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private const string API_KEY = "test-api-key";

    public PerspectiveApiServiceTest()
    {
        _loggerMock = new Mock<ILogger<PerspectiveApiService>>();
        _configurationMock = new Mock<IConfiguration>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _aiLogService = new Mock<IAiLogService>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        // Setup configuration
        _configurationMock.Setup(c => c["PerspectiveApi:ApiKey"]).Returns(API_KEY);

        // Setup HttpClient with mock handler
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        // Setup scope -> provider -> service
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IAiLogService)))
            .Returns(_aiLogService.Object);

        _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceScopeFactoryMock
            .Setup(f => f.CreateScope())
            .Returns(_serviceScopeMock.Object);

        // Create service under test
        _service = new PerspectiveApiService(
            _httpClient,
            _configurationMock.Object,
            _loggerMock.Object,
            _serviceScopeFactoryMock.Object);
    }
    
    [Fact]
    public async Task AnalyzeTextAsync_WhenApiKeyIsNull_ReturnsFailureResult()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["PerspectiveApi:ApiKey"]).Returns((string)null);

        var service = new PerspectiveApiService(_httpClient, configMock.Object, _loggerMock.Object, _serviceScopeFactoryMock.Object);

        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuizRatingFeedback,
            ReportComment = "Test comment"
        };

        // Act
        var result = await service.AnalyzeTextAsync(reportData);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Perspective API key is not configured.", result.ErrorMessage);
        Assert.Equal(reportData.ReportId, result.ReportId);
        Assert.Equal(reportData.ReportType, result.ReportType);
    }

    [Fact]
    public async Task AnalyzeTextAsync_WhenApiReturnsError_ReturnsFailureResult()
    {
        // Arrange
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuizRatingFeedback,
            ReportComment = "Test comment"
        };

        var errorResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent("Bad Request Error")
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(errorResponse);

        // Act
        var result = await _service.AnalyzeTextAsync(reportData);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("BadRequest", result.ErrorMessage);
        Assert.Contains("Bad Request Error", result.ErrorMessage);
    }

    [Fact]
    public async Task AnalyzeTextAsync_QuizRatingFeedback_WithHighToxicity_ReturnsFlaggedResult()
    {
        // Arrange
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuizRatingFeedback,
            ReportComment = "This is toxic comment"
        };

        var apiResponse = new PerspectiveApiResponse
        {
            AttributeScores = new Dictionary<string, AttributeScore>
            {
                ["TOXICITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.8 }
                },
                ["SEVERE_TOXICITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.3 }
                },
                ["INSULT"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.2 }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);

        // Act
        var result = await _service.AnalyzeTextAsync(reportData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsFlagged);
        Assert.Equal(0.8, result.AttributeScore);
        Assert.NotNull(result.FlagReason);
        Assert.Equal("TOXICITY", result.AttributeName);
    }

    [Fact]
    public async Task AnalyzeTextAsync_QuizRatingFeedback_WithLowToxicity_ReturnsNotFlaggedResult()
    {
        // Arrange
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuizRatingFeedback,
            ReportComment = "This is a nice quiz"
        };

        var apiResponse = new PerspectiveApiResponse
        {
            AttributeScores = new Dictionary<string, AttributeScore>
            {
                ["TOXICITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.1 }
                },
                ["SEVERE_TOXICITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.05 }
                },
                ["INSULT"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.08 }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);

        // Act
        var result = await _service.AnalyzeTextAsync(reportData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFlagged);
        Assert.Null(result.FlagReason);
        Assert.Equal(0.1, result.AttributeScore); // Highest score
    }

    [Fact]
    public async Task AnalyzeTextAsync_QuestionIssueReport_CalculatesSeverityCorrectly_High()
    {
        // Arrange
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuestionIssueReport,
            ReportComment = "This question is terrible"
        };

        var apiResponse = new PerspectiveApiResponse
        {
            AttributeScores = new Dictionary<string, AttributeScore>
            {
                ["TOXICITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.8 }
                },
                ["PROFANITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.7 }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);

        // Act
        var result = await _service.AnalyzeTextAsync(reportData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(QuestionOrQuizIssueReportSeverity.High, result.Severity);
        Assert.Equal(0.8, result.AttributeScore);
    }

    [Fact]
    public async Task AnalyzeTextAsync_QuestionIssueReport_CalculatesSeverityCorrectly_Medium()
    {
        // Arrange
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuestionIssueReport,
            ReportComment = "This question needs work"
        };

        var apiResponse = new PerspectiveApiResponse
        {
            AttributeScores = new Dictionary<string, AttributeScore>
            {
                ["TOXICITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.5 }
                },
                ["PROFANITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.6 }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);

        // Act
        var result = await _service.AnalyzeTextAsync(reportData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(QuestionOrQuizIssueReportSeverity.Medium, result.Severity);
        Assert.Equal(0.6, result.AttributeScore);
    }

    [Fact]
    public async Task AnalyzeTextAsync_QuestionIssueReport_CalculatesSeverityCorrectly_Low()
    {
        // Arrange
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuestionIssueReport,
            ReportComment = "Minor issue with question"
        };

        // Use only attributes from QuizAndQuestionGcpAttributes: TOXICITY, ATTACK_ON_AUTHOR, PROFANITY, THREAT
        var apiResponse = new PerspectiveApiResponse
        {
            AttributeScores = new Dictionary<string, AttributeScore>
            {
                ["TOXICITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.2 }
                },
                ["PROFANITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.1 }
                },
                ["ATTACK_ON_AUTHOR"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.15 }
                },
                ["THREAT"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.05 }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);

        // Act
        var result = await _service.AnalyzeTextAsync(reportData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(QuestionOrQuizIssueReportSeverity.Low, result.Severity);
        Assert.Equal(0.2, result.AttributeScore);
    }

    [Fact]
    public async Task AnalyzeTextAsync_QuizIssueReport_CalculatesSeverityCorrectly()
    {
        // Arrange
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuizIssueReport,
            ReportComment = "Quiz has problems"
        };

        var apiResponse = new PerspectiveApiResponse
        {
            AttributeScores = new Dictionary<string, AttributeScore>
            {
                ["TOXICITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.45 }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);

        // Act
        var result = await _service.AnalyzeTextAsync(reportData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(QuestionOrQuizIssueReportSeverity.Medium, result.Severity);
    }

    [Fact]
    public async Task AnalyzeTextAsync_WhenExceptionThrown_ReturnsFailureResult()
    {
        // Arrange
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuizRatingFeedback,
            ReportComment = "Test comment"
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.AnalyzeTextAsync(reportData);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Network error", result.ErrorMessage);
    }

    [Fact]
    public async Task AnalyzeTextAsync_WithNullAttributeScores_ReturnsZeroScore()
    {
        // Arrange
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuizRatingFeedback,
            ReportComment = "Test comment"
        };

        var apiResponse = new PerspectiveApiResponse
        {
            AttributeScores = null
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);

        // Act
        var result = await _service.AnalyzeTextAsync(reportData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.AttributeScore);
    }

    [Fact]
    public async Task AnalyzeTextAsync_WithEmptyAttributeScores_ReturnsZeroScore()
    {
        // Arrange
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuizRatingFeedback,
            ReportComment = "Test comment"
        };

        var apiResponse = new PerspectiveApiResponse
        {
            AttributeScores = new Dictionary<string, AttributeScore>()
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);

        // Act
        var result = await _service.AnalyzeTextAsync(reportData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.AttributeScore);
        Assert.False(result.IsFlagged);
    }

    [Fact]
    public async Task AnalyzeTextAsync_QuizRatingFeedback_WithBoundaryScore_FlagsCorrectly()
    {
        // Arrange - Testing boundary at 0.4
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuizRatingFeedback,
            ReportComment = "Boundary test"
        };

        var apiResponse = new PerspectiveApiResponse
        {
            AttributeScores = new Dictionary<string, AttributeScore>
            {
                ["TOXICITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.41 }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);

        // Act
        var result = await _service.AnalyzeTextAsync(reportData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsFlagged);
        Assert.Equal(0.41, result.AttributeScore);
    }

    [Fact]
    public async Task AnalyzeTextAsync_SeverityBoundaries_HighSeverity()
    {
        // Arrange - Testing boundary at 0.7
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuestionIssueReport,
            ReportComment = "High severity test"
        };

        var apiResponse = new PerspectiveApiResponse
        {
            AttributeScores = new Dictionary<string, AttributeScore>
            {
                ["TOXICITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.71 }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);

        // Act
        var result = await _service.AnalyzeTextAsync(reportData);

        // Assert
        Assert.Equal(QuestionOrQuizIssueReportSeverity.High, result.Severity);
    }

    [Fact]
    public async Task AnalyzeTextAsync_MultipleAttributes_SelectsHighestScore()
    {
        // Arrange
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuizRatingFeedback,
            ReportComment = "Test multiple attributes"
        };

        // Use only attributes from QuizRatingGcpAttributes: TOXICITY, ATTACK_ON_AUTHOR, PROFANITY, THREAT, SPAM, INSULT
        var apiResponse = new PerspectiveApiResponse
        {
            AttributeScores = new Dictionary<string, AttributeScore>
            {
                ["TOXICITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.3 }
                },
                ["SPAM"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.9 }
                },
                ["INSULT"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.2 }
                },
                ["PROFANITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.15 }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);

        // Act
        var result = await _service.AnalyzeTextAsync(reportData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("SPAM", result.AttributeName); // Highest score
        Assert.Equal(0.9, result.AttributeScore);
        Assert.True(result.IsFlagged);
    }

    [Fact]
    public async Task AnalyzeTextAsync_CallsAiLogService_StartAndEnd()
    {
        // Arrange
        var reportData = new GcpApiReportDataDto
        {
            ReportId = 1,
            ReportType = ReportType.QuizRatingFeedback,
            ReportComment = "Simple test"
        };

        var apiResponse = new PerspectiveApiResponse
        {
            AttributeScores = new Dictionary<string, AttributeScore>
            {
                ["TOXICITY"] = new AttributeScore
                {
                    SummaryScore = new SummaryScore { Value = 0.2 }
                }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, apiResponse);

        // Act
        var result = await _service.AnalyzeTextAsync(reportData);

        // Assert
        Assert.True(result.IsSuccess);
        _aiLogService.Verify(x => x.StartApiCall(It.IsAny<AiApiCallStartDetail>()), Times.Once);
        _aiLogService.Verify(x => x.EndApiCall(It.IsAny<AiProcessLog>(), true), Times.Once);
    }

    // Helper method to setup HTTP response
    private void SetupHttpResponse(HttpStatusCode statusCode, object content)
    {
        var response = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = JsonContent.Create(content)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}