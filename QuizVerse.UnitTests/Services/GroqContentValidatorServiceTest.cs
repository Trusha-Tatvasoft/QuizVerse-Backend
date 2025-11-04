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

    public GroqContentValidatorServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockConfiguration = new Mock<IConfiguration>();
        _mockModelRotationService = new Mock<IGroqModelRotationService>();

        _mockConfiguration.Setup(x => x["Groq:ValidatorApiKey"]).Returns("test-validator-key");
        _mockConfiguration.Setup(x => x["Groq:ApiKey"]).Returns("test-api-key");

        _validatorService = new GroqContentValidatorService(_httpClient, _mockConfiguration.Object, _mockModelRotationService.Object);
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

        var apiResponse = new
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
            usage = new { total_tokens = 150 }
        };

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModel();

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

        var apiResponse = new
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
            usage = new { total_tokens = 120 }
        };

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModel();

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
    public async Task ValidateContentAsync_WithRateLimit_RetriesWithBackoff()
    {
        // Arrange
        var content = "Science content";
        var category = "educational";
        var modelConfig = new ModelConfig("test-model", 1000, 10000, 100000, 1000000);

        var validationResponse = new
        {
            isValid = true,
            isMatch = true,
            detectedCategory = "educational",
            reason = "Valid content"
        };

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
                        Content = new StringContent(JsonSerializer.Serialize(new
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
                            usage = new { total_tokens = 100 }
                        }))
                    };
                }
            });

        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);
        _mockModelRotationService.Setup(x => x.HandleRateLimitExceeded(It.IsAny<string>()));

        // Act
        var result = await _validatorService.ValidateContentAsync(content, category);

        // Assert
        _mockModelRotationService.Verify(x => x.HandleRateLimitExceeded("test-model"), Times.Once);
        Assert.True(result.IsValid);
        Assert.True(result.IsMatch);
    }

    [Fact]
    public async Task ValidateContentAsync_WithServerError_Retries()
    {
        // Arrange
        var content = "Science content";
        var category = "educational";
        var modelConfig = new ModelConfig("test-model", 1000, 10000, 100000, 1000000);

        var validationResponse = new
        {
            isValid = true,
            isMatch = true,
            detectedCategory = "educational",
            reason = "Valid content"
        };

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
                        Content = new StringContent(JsonSerializer.Serialize(new
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
                            usage = new { total_tokens = 100 }
                        }))
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
        var modelConfig = new ModelConfig("test-model", 1000, 10000, 100000, 1000000);

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
        SetupAvailableModel();

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
        SetupAvailableModel();

        // Act
        var result = await _validatorService.ValidateContentAsync(content, category);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.IsMatch);
        Assert.Equal("unknown", result.Category);
        Assert.Contains("parse", result.Reason); // Check if reason contains "parse"
        Assert.True(result.ValidationFailed);
    }
    [Fact]
    public async Task ValidateContentAsync_WithNoAvailableModel_RetriesAfterDelay()
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

        var apiResponse = new
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
            usage = new { total_tokens = 100 }
        };

        SetupSuccessfulApiResponse(apiResponse);

        // First call: no model available, second call: model available
        _mockModelRotationService.SetupSequence(x => x.GetAvailableModel())
            .Returns((ModelConfig)null)
            .Returns(new ModelConfig("test-model", 1000, 10000, 100000, 1000000));

        // Act
        var result = await _validatorService.ValidateContentAsync(content, category);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.IsMatch);
    }

    [Fact]
    public async Task ValidateContentAsync_WithHttpRequestException_Retries()
    {
        // Arrange
        var content = "Science content";
        var category = "educational";
        var modelConfig = new ModelConfig("test-model", 1000, 10000, 100000, 1000000);

        var validationResponse = new
        {
            isValid = true,
            isMatch = true,
            detectedCategory = "educational",
            reason = "Valid content"
        };

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
                        Content = new StringContent(JsonSerializer.Serialize(new
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
                            usage = new { total_tokens = 100 }
                        }))
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
        var modelConfig = new ModelConfig("test-model", 1000, 10000, 100000, 1000000);

        var validationResponse = new
        {
            isValid = true,
            isMatch = true,
            detectedCategory = "educational",
            reason = "Valid content"
        };

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
                        Content = new StringContent(JsonSerializer.Serialize(new
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
                            usage = new { total_tokens = 100 }
                        }))
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
        var longContent = new string('A', 3000); // 3000 characters
        var category = "educational";

        var validationResponse = new
        {
            isValid = true,
            isMatch = true,
            detectedCategory = "educational",
            reason = "Valid content"
        };

        var apiResponse = new
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
            usage = new { total_tokens = 100 }
        };

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModel();

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

        var apiResponse = new
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
            usage = new { total_tokens = 100 }
        };

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModel();

        // Act
        var result = await _validatorService.ValidateContentAsync(content);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.IsMatch);
        Assert.Equal("educational", result.RequestedCategory);
    }

    // Tests for private methods using reflection
    [Fact]
    public void ExtractTokenUsage_WithValidUsage_ReturnsTokens()
    {
        // Arrange
        var responseJson = @"{
            ""usage"": {
                ""total_tokens"": 250
            }
        }";

        // Act
        var tokens = InvokePrivateMethod<int>("ExtractTokenUsage", responseJson);

        // Assert
        Assert.Equal(250, tokens);
    }

    [Fact]
    public void ExtractTokenUsage_WithMissingUsage_ReturnsDefault()
    {
        // Arrange
        var responseJson = @"{
            ""choices"": []
        }";

        // Act
        var tokens = InvokePrivateMethod<int>("ExtractTokenUsage", responseJson);

        // Assert
        Assert.Equal(200, tokens); // Default value for validator service
    }

    [Fact]
    public void ParseValidationResponse_WithValidJson_ReturnsCorrectResult()
    {
        // Arrange
        var validationJson = JsonSerializer.Serialize(new
        {
            isValid = false,
            isMatch = false,
            detectedCategory = "inappropriate",
            reason = "Invalid content"
        });

        var apiResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = validationJson
                    }
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(apiResponse);

        // Act
        var result = InvokePrivateMethod<ContentValidationResult>("ParseValidationResponse", jsonResponse, "educational");

        // Assert
        Assert.False(result.IsValid);
        Assert.False(result.IsMatch);
        Assert.Equal("inappropriate", result.Category);
        Assert.Equal("Invalid content", result.Reason);
        Assert.False(result.ValidationFailed);
    }

    private T InvokePrivateMethod<T>(string methodName, params object[] parameters)
    {
        var method = typeof(GroqContentValidatorService).GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method == null)
        {
            throw new ArgumentException($"Method {methodName} not found");
        }

        return (T)method.Invoke(_validatorService, parameters);
    }

    private void SetupAvailableModel()
    {
        var modelConfig = new ModelConfig("test-model", 1000, 10000, 100000, 1000000);
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
}