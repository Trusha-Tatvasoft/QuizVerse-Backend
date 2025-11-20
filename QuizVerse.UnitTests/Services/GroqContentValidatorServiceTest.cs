using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class GroqContentValidatorServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IGroqModelRotationService> _mockModelRotationService;
    private readonly GroqContentValidatorService _validatorService;
    private readonly Mock<IAiLogService> _aiLogServiceMock;

    public GroqContentValidatorServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockConfiguration = new Mock<IConfiguration>();
        _mockModelRotationService = new Mock<IGroqModelRotationService>();
        _aiLogServiceMock = new Mock<IAiLogService>();
        _mockConfiguration.Setup(x => x["Groq:ValidatorApiKey"]).Returns("test-validator-key");
        _mockConfiguration.Setup(x => x["Groq:ApiKey"]).Returns("test-api-key");

        // Setup AI log service mocks
        _aiLogServiceMock.Setup(x => x.StartApiCall(It.IsAny<AiApiCallStartDetail>()))
            .Returns(new QuizVerse.Domain.Entities.AiProcessLog());
        _aiLogServiceMock.Setup(x => x.EndApiCall(It.IsAny<QuizVerse.Domain.Entities.AiProcessLog>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        _validatorService = new GroqContentValidatorService(_httpClient, _mockConfiguration.Object, _mockModelRotationService.Object, _aiLogServiceMock.Object);
    }

    [Fact]
    public async Task ValidateContentAsync_WithValidResponse_ReturnsValidationResult()
    {
        // Arrange
        var content = "Science content about physics";
        var category = "educational";

        var validationResponse = new
        {
            isValid = true,
            isMatch = true,
            detectedCategory = "educational",
            reason = "Content matches educational criteria"
        };

        var apiResponse = CreateGroqApiResponse(validationResponse, 150);

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModelWithTokens("llama-3.1-8b-instant", estimatedTokens: 200);

        // Act
        var result = await _validatorService.ValidateContentAsync(content, category);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.IsMatch);
        Assert.Equal("educational", result.Category);
        Assert.Equal("Content matches educational criteria", result.Reason);
        Assert.False(result.ValidationFailed);
        Assert.Equal(category, result.RequestedCategory);
        _mockModelRotationService.Verify(x => x.RecordUsage(It.IsAny<string>(), 150), Times.Once);
    }

    [Fact]
    public async Task ValidateContentAsync_WithNoModelAvailable_RetriesAfterDelay()
    {
        // Arrange
        var content = "Science content";
        var category = "educational";

        var validationResponse = new
        {
            isValid = true,
            isMatch = true,
            detectedCategory = "educational",
            reason = "Valid content"
        };

        var apiResponse = CreateGroqApiResponse(validationResponse, 100);

        SetupSuccessfulApiResponse(apiResponse);

        var callCount = 0;
        _mockModelRotationService.Setup(x => x.GetAvailableModel())
            .Returns(() =>
            {
                callCount++;
                return callCount == 1 ? null : new ModelConfig("llama-3.1-8b-instant", 1000, 10000, 100000, 1000000);
            });

        // Act
        var result = await _validatorService.ValidateContentAsync(content, category);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.IsMatch);
    }

    [Fact]
    public async Task ValidateContentAsync_WithMultipleInFlightRequests_ManagesThrottling()
    {
        // Arrange
        var content = "Science content";
        var category = "educational";

        var validationResponse = new
        {
            isValid = true,
            isMatch = true,
            detectedCategory = "educational",
            reason = "Valid content"
        };

        var apiResponse = CreateGroqApiResponse(validationResponse, 100);

        // Setup multiple successful responses
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(apiResponse))
            });

        var model = new ModelConfig("llama-3.1-8b-instant", 5000, 10000, 100000, 1000000);
        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(model);

        var results = new List<ContentValidationResult>();
        for (int i = 0; i < 3; i++)
        {
            var result = await _validatorService.ValidateContentAsync(content + i, category);
            results.Add(result);
        }

        // Assert
        Assert.All(results, result => Assert.True(result.IsValid));
        _mockModelRotationService.Verify(x => x.RecordUsage(It.IsAny<string>(), 100), Times.Exactly(3));
    }

    [Fact]
    public async Task ValidateContentAsync_WithRateLimit_RetriesWithBackoffAndDecrementsInFlight()
    {
        // Arrange
        var content = "Science content";
        var category = "educational";
        var modelConfig = new ModelConfig("llama-3.1-8b-instant", 1000, 10000, 100000, 1000000);

        var validationResponse = new
        {
            isValid = true,
            isMatch = true,
            detectedCategory = "educational",
            reason = "Valid content"
        };

        var apiResponse = CreateGroqApiResponse(validationResponse, 100);

        var callCount = 0;
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.TooManyRequests,
                        Content = new StringContent("Rate limit exceeded")
                    };
                }
                else
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(apiResponse))
                    };
                }
            });

        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);
        _mockModelRotationService.Setup(x => x.HandleRateLimitExceeded(It.IsAny<string>())).Callback(() => { });

        // Act
        var result = await _validatorService.ValidateContentAsync(content, category);

        // Assert
        _mockModelRotationService.Verify(x => x.HandleRateLimitExceeded("llama-3.1-8b-instant"), Times.Once);
        Assert.True(result.IsValid);
        Assert.True(result.IsMatch);
    }

    [Fact]
    public async Task ValidateContentAsync_WithServerError_RetriesAndDecrementsInFlight()
    {
        // Arrange
        var content = "Science content";
        var category = "educational";
        var modelConfig = new ModelConfig("llama-3.1-8b-instant", 1000, 10000, 100000, 1000000);

        var validationResponse = new
        {
            isValid = true,
            isMatch = true,
            detectedCategory = "educational",
            reason = "Valid content"
        };

        var apiResponse = CreateGroqApiResponse(validationResponse, 100);

        var callCount = 0;
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        Content = new StringContent("Server error")
                    };
                }
                else
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(apiResponse))
                    };
                }
            });

        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);

        // Act
        var result = await _validatorService.ValidateContentAsync(content, category);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.IsMatch);
    }

    [Fact]
    public async Task ValidateContentAsync_WithMaxRetriesExceeded_ReturnsFallbackResult()
    {
        // Arrange
        var content = "Science content";
        var category = "educational";

        var modelConfig = new ModelConfig("llama-3.1-8b-instant", 1000, 10000, 100000, 1000000);

        // Always return server error to trigger max retries
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Server error")
            });

        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);

        // Act
        var result = await _validatorService.ValidateContentAsync(content, category);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.IsMatch);
        Assert.Equal("unknown", result.Category);
        Assert.Equal(Constants.MAX_RETRIES_REACHED_DURING_VALIDATION, result.Reason);
        Assert.True(result.ValidationFailed);
    }

    [Fact]
    public async Task ValidateContentAsync_WithInvalidContent_ReturnsFalseValidation()
    {
        // Arrange
        var content = "Inappropriate content";
        var category = "educational";

        var validationResponse = new
        {
            isValid = false,
            isMatch = false,
            detectedCategory = "inappropriate",
            reason = "Content contains inappropriate material"
        };

        var apiResponse = CreateGroqApiResponse(validationResponse, 120);

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModelWithTokens("llama-3.1-8b-instant", estimatedTokens: 200);

        // Act
        var result = await _validatorService.ValidateContentAsync(content, category);

        // Assert
        Assert.False(result.IsValid);
        Assert.False(result.IsMatch);
        Assert.Equal("inappropriate", result.Category);
        Assert.Equal("Content contains inappropriate material", result.Reason);
        Assert.False(result.ValidationFailed);
    }

    [Fact]
    public async Task ValidateContentAsync_WithJsonWrappedInMarkdown_CleansResponse()
    {
        // Arrange
        var content = "Science content";
        var category = "educational";

        var validationResponse = new
        {
            isValid = true,
            isMatch = true,
            detectedCategory = "educational",
            reason = "Valid content"
        };

        var wrappedJson = $"```json\n{JsonSerializer.Serialize(validationResponse)}\n```";

        var apiResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = wrappedJson
                    }
                }
            },
            usage = new { total_tokens = 100 }
        };

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModelWithTokens("llama-3.1-8b-instant", estimatedTokens: 200);

        // Act
        var result = await _validatorService.ValidateContentAsync(content, category);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.IsMatch);
        Assert.Equal("educational", result.Category);
    }

    [Fact]
    public async Task ValidateContentAsync_WithMalformedJsonResponse_ReturnsFallback()
    {
        // Arrange
        var content = "Science content";
        var category = "educational";

        var apiResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "Invalid JSON {"
                    }
                }
            },
            usage = new { total_tokens = 100 }
        };

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModelWithTokens("llama-3.1-8b-instant", estimatedTokens: 200);

        // Act
        var result = await _validatorService.ValidateContentAsync(content, category);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.IsMatch);
        Assert.Equal("unknown", result.Category);
        Assert.Contains("parse", result.Reason.ToLower());
        Assert.True(result.ValidationFailed);
    }

    [Fact]
    public async Task ValidateContentAsync_WithHttpRequestException_Retries()
    {
        // Arrange
        var content = "Science content";
        var category = "educational";
        var modelConfig = new ModelConfig("llama-3.1-8b-instant", 1000, 10000, 100000, 1000000);

        var validationResponse = new
        {
            isValid = true,
            isMatch = true,
            detectedCategory = "educational",
            reason = "Valid content"
        };

        var apiResponse = CreateGroqApiResponse(validationResponse, 100);

        var callCount = 0;
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new HttpRequestException("Network error");
                }
                else
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(apiResponse))
                    };
                }
            });

        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);

        // Act
        var result = await _validatorService.ValidateContentAsync(content, category);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.IsMatch);
    }

    [Fact]
    public async Task ValidateContentAsync_WithTaskCanceledException_Retries()
    {
        // Arrange
        var content = "Science content";
        var category = "educational";
        var modelConfig = new ModelConfig("llama-3.1-8b-instant", 1000, 10000, 100000, 1000000);

        var validationResponse = new
        {
            isValid = true,
            isMatch = true,
            detectedCategory = "educational",
            reason = "Valid content"
        };

        var apiResponse = CreateGroqApiResponse(validationResponse, 100);

        var callCount = 0;
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new TaskCanceledException("Request timed out");
                }
                else
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(apiResponse))
                    };
                }
            });

        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);

        // Act
        var result = await _validatorService.ValidateContentAsync(content, category);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.IsMatch);
    }

    [Fact]
    public async Task ValidateContentAsync_WithLongContent_TruncatesContent()
    {
        // Arrange
        var longContent = new string('A', 3000);
        var category = "educational";

        var validationResponse = new
        {
            isValid = true,
            isMatch = true,
            detectedCategory = "educational",
            reason = "Valid content"
        };

        var apiResponse = CreateGroqApiResponse(validationResponse, 100);

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModelWithTokens("llama-3.1-8b-instant", estimatedTokens: 200);

        // Act
        var result = await _validatorService.ValidateContentAsync(longContent, category);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.IsMatch);
    }

    [Fact]
    public async Task ValidateContentAsync_WithDefaultCategory_UsesEducational()
    {
        // Arrange
        var content = "Science content";

        var validationResponse = new
        {
            isValid = true,
            isMatch = true,
            detectedCategory = "educational",
            reason = "Valid content"
        };

        var apiResponse = CreateGroqApiResponse(validationResponse, 100);

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModelWithTokens("llama-3.1-8b-instant", estimatedTokens: 200);

        // Act
        var result = await _validatorService.ValidateContentAsync(content);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.IsMatch);
        Assert.Equal("educational", result.RequestedCategory);
    }

    private void SetupAvailableModelWithTokens(string modelName, int estimatedTokens = 200)
    {
        var modelConfig = new ModelConfig(modelName, 1000, 10000, 100000, 1000000);
        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);
    }

    private HttpResponseMessage SetupSuccessfulApiResponse(object responseObject)
    {
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(responseObject))
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return response;
    }

    private object CreateGroqApiResponse(object validationResponse, int totalTokens)
    {
        return new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = JsonSerializer.Serialize(validationResponse)
                    }
                }
            },
            usage = new { total_tokens = totalTokens }
        };
    }
}