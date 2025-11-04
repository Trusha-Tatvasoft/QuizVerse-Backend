using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Common;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class GroqServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IGroqModelRotationService> _mockModelRotationService;
    private readonly GroqService _groqService;

    public GroqServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockConfiguration = new Mock<IConfiguration>();
        _mockModelRotationService = new Mock<IGroqModelRotationService>();

        _mockConfiguration.Setup(x => x["Groq:ApiKey"]).Returns("test-api-key");

        _groqService = new GroqService(_httpClient, _mockConfiguration.Object, _mockModelRotationService.Object);
    }

    [Fact]
    public async Task GenerateQuestions_WithValidResponse_ReturnsCleanedJson()
    {
        // Arrange
        var prompt = "Generate 5 quiz questions about science";
        var expectedResponse = @"[
            {
                ""question"": ""What is the chemical symbol for water?"",
                ""options"": [""H2O"", ""CO2"", ""NaCl"", ""O2""],
                ""answer"": ""H2O""
            }
        ]";

        var apiResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = expectedResponse
                    }
                }
            },
            usage = new
            {
                total_tokens = 150
            }
        };

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModel();

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        Assert.Equal(expectedResponse, result);
        _mockModelRotationService.Verify(x => x.RecordUsage(It.IsAny<string>(), 150), Times.Once);
    }

    [Fact]
    public async Task GenerateQuestions_WithRateLimit_RetriesWithBackoff()
    {
        // Arrange
        var prompt = "Generate quiz questions";
        var modelConfig = new ModelConfig("test-model", 1000, 10000, 100000, 1000000);

        // Setup sequence: first call rate limited, second call successful
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
                    // First call: rate limited
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.TooManyRequests,
                        Content = new StringContent("Rate limit exceeded")
                    };
                }
                else
                {
                    // Second call: success
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
                                    content = "[{}]"
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
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        _mockModelRotationService.Verify(x => x.HandleRateLimitExceeded("test-model"), Times.Once);
        _mockModelRotationService.Verify(x => x.RecordUsage("test-model", 100), Times.Once);
        Assert.Equal("[{}]", result);
    }
    [Fact]
    public async Task GenerateQuestions_WithServerError_Retries()
    {
        // Arrange
        var prompt = "Generate quiz questions";
        var modelConfig = new ModelConfig("test-model", 1000, 10000, 100000, 1000000);

        // First call: server error, second call: success
        var serverErrorResponse = SetupHttpResponse(HttpStatusCode.InternalServerError, "Server error");
        var successResponse = SetupSuccessfulApiResponse(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "[{}]"
                    }
                }
            },
            usage = new { total_tokens = 100 }
        });

        _mockModelRotationService.SetupSequence(x => x.GetAvailableModel())
            .Returns(modelConfig)
            .Returns(modelConfig);

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        Assert.Equal("[{}]", result);
    }

    [Fact]
    public async Task GenerateQuestions_WithEmptyResponse_Retries()
    {
        // Arrange
        var prompt = "Generate quiz questions";
        var modelConfig = new ModelConfig("test-model", 1000, 10000, 100000, 1000000);

        // First call: empty content, second call: success
        var emptyResponse = SetupSuccessfulApiResponse(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "[]"
                    }
                }
            },
            usage = new { total_tokens = 50 }
        });

        var successResponse = SetupSuccessfulApiResponse(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "[{\"question\": \"Test?\"}]"
                    }
                }
            },
            usage = new { total_tokens = 100 }
        });

        _mockModelRotationService.SetupSequence(x => x.GetAvailableModel())
            .Returns(modelConfig)
            .Returns(modelConfig);

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        Assert.Equal("[{\"question\": \"Test?\"}]", result);
    }

    [Fact]
    public async Task GenerateQuestions_WithMarkdownWrappedJson_CleansResponse()
    {
        // Arrange
        var prompt = "Generate quiz questions";
        var wrappedResponse = "```json\n[{\"question\": \"Test?\"}]\n```";
        var expectedCleanResponse = "[{\"question\": \"Test?\"}]";

        var apiResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = wrappedResponse
                    }
                }
            },
            usage = new { total_tokens = 100 }
        };

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModel();

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        Assert.Equal(expectedCleanResponse, result);
    }

    [Fact]
    public async Task GenerateQuestions_WithNestedJsonArray_ExtractsCorrectArray()
    {
        // Arrange
        var prompt = "Generate quiz questions";
        var nestedResponse = new
        {
            questions = new[]
            {
                new { question = "Q1?" },
                new { question = "Q2?" }
            }
        };

        var apiResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = JsonSerializer.Serialize(nestedResponse)
                    }
                }
            },
            usage = new { total_tokens = 100 }
        };

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModel();

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        var expected = JsonSerializer.Serialize(nestedResponse.questions);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GenerateQuestions_ExceedsMaxRetries_ThrowsException()
    {
        // Arrange
        var prompt = "Generate quiz questions";
        var modelConfig = new ModelConfig("test-model", 1000, 10000, 100000, 1000000);

        // Always return rate limit
        var rateLimitResponse = SetupHttpResponse(HttpStatusCode.TooManyRequests, "Rate limit exceeded");

        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);
        _mockModelRotationService.Setup(x => x.HandleRateLimitExceeded(It.IsAny<string>()));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _groqService.GenerateQuesions(prompt));
    }

    [Fact]
    public async Task GenerateQuestions_WithNoAvailableModel_RetriesAfterDelay()
    {
        // Arrange
        var prompt = "Generate quiz questions";

        // First call: no model available, second call: model available
        _mockModelRotationService.SetupSequence(x => x.GetAvailableModel())
            .Returns((ModelConfig)null)
            .Returns(new ModelConfig("test-model", 1000, 10000, 100000, 1000000));

        SetupSuccessfulApiResponse(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "[{}]"
                    }
                }
            },
            usage = new { total_tokens = 100 }
        });

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        Assert.Equal("[{}]", result);
    }

    [Fact]
    public async Task GenerateQuestions_WithHttpRequestException_Retries()
    {
        // Arrange
        var prompt = "Generate quiz questions";
        var modelConfig = new ModelConfig("test-model", 1000, 10000, 100000, 1000000);

        // First call: network error, second call: success
        _mockHttpMessageHandler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"))
            .ReturnsAsync(new HttpResponseMessage
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
                                content = "[{}]"
                            }
                        }
                    },
                    usage = new { total_tokens = 100 }
                }))
            });

        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        Assert.Equal("[{}]", result);
    }

    [Fact]
    public async Task GenerateQuestions_WithTaskCanceledException_Retries()
    {
        // Arrange
        var prompt = "Generate quiz questions";
        var modelConfig = new ModelConfig("test-model", 1000, 10000, 100000, 1000000);

        // First call: timeout, second call: success
        _mockHttpMessageHandler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"))
            .ReturnsAsync(new HttpResponseMessage
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
                                content = "[{}]"
                            }
                        }
                    },
                    usage = new { total_tokens = 100 }
                }))
            });

        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        Assert.Equal("[{}]", result);
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
    public void CleanJsonResponse_WithMalformedJson_ExtractsArrayWithRegex()
    {
        // Arrange
        var malformedJson = "Some text before [{\"question\": \"Test?\"}] some text after";

        // Act
        var result = InvokePrivateMethod<string>("CleanJsonResponse", malformedJson);

        // Assert
        Assert.Equal("[{\"question\": \"Test?\"}]", result);
    }

    [Fact]
    public void CleanJsonResponse_WithJsonWrappedInMarkdown_RemovesMarkdown()
    {
        // Arrange
        var wrappedJson = "```json\n[{\"question\": \"Test?\"}]\n```";

        // Act
        var result = InvokePrivateMethod<string>("CleanJsonResponse", wrappedJson);

        // Assert
        Assert.Equal("[{\"question\": \"Test?\"}]", result);
    }

    [Fact]
    public void CleanJsonResponse_WithEmptyString_ReturnsEmptyArray()
    {
        // Arrange
        var emptyString = "";

        // Act
        var result = InvokePrivateMethod<string>("CleanJsonResponse", emptyString);

        // Assert
        Assert.Equal("[]", result);
    }

    private T InvokePrivateMethod<T>(string methodName, params object[] parameters)
    {
        var method = typeof(GroqService).GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method == null)
        {
            throw new ArgumentException($"Method {methodName} not found");
        }

        return (T)method.Invoke(_groqService, parameters);
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

    private HttpResponseMessage SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(content)
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