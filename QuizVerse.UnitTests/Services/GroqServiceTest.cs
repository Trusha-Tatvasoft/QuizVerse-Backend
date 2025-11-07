using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Domain.Entities;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class GroqServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IGroqModelRotationService> _mockModelRotationService;
    private readonly Mock<IAiLogService> _mockAiLogService;
    private readonly GroqService _groqService;

    public GroqServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockConfiguration = new Mock<IConfiguration>();
        _mockModelRotationService = new Mock<IGroqModelRotationService>();
        _mockAiLogService = new Mock<IAiLogService>();

        _mockConfiguration.Setup(x => x["Groq:ApiKey"]).Returns("test-api-key");

        _groqService = new GroqService(_httpClient, _mockConfiguration.Object, _mockModelRotationService.Object, _mockAiLogService.Object);
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

        var aiProcessLog = new AiProcessLog { Id = 123 };
        _mockAiLogService.Setup(x => x.StartApiCall(It.IsAny<AiApiCallStartDetail>())).Returns(aiProcessLog);
        _mockAiLogService.Setup(x => x.EndApiCall(aiProcessLog, true)).Returns(Task.CompletedTask);

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModel("llama-3.1-8b-instant");

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        Assert.Equal(expectedResponse, result.ContentString);
        Assert.Equal(123, result.AILogId);
        _mockModelRotationService.Verify(x => x.RecordUsage(It.IsAny<string>(), 150), Times.Once);
        _mockAiLogService.Verify(x => x.StartApiCall(It.IsAny<AiApiCallStartDetail>()), Times.Once);
        _mockAiLogService.Verify(x => x.EndApiCall(aiProcessLog, true), Times.Once);
    }

    [Fact]
    public async Task GenerateQuestions_WithRateLimit_RetriesWithBackoff()
    {
        // Arrange
        var prompt = "Generate quiz questions";
        var modelConfig = new ModelConfig("llama-3.1-8b-instant", 1000, 10000, 100000, 1000000);

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
                                        content = "[{}]"
                                    }
                                }
                            },
                            usage = new { total_tokens = 100 }
                        }))
                    };
                }
            });

        var aiProcessLog1 = new AiProcessLog { Id = 123 };
        var aiProcessLog2 = new AiProcessLog { Id = 456 };
        _mockAiLogService.SetupSequence(x => x.StartApiCall(It.IsAny<AiApiCallStartDetail>()))
            .Returns(aiProcessLog1)
            .Returns(aiProcessLog2);

        _mockAiLogService.Setup(x => x.EndApiCall(aiProcessLog1, false)).Returns(Task.CompletedTask);
        _mockAiLogService.Setup(x => x.EndApiCall(aiProcessLog2, true)).Returns(Task.CompletedTask);

        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);
        _mockModelRotationService.Setup(x => x.HandleRateLimitExceeded(It.IsAny<string>()));

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        _mockModelRotationService.Verify(x => x.HandleRateLimitExceeded("llama-3.1-8b-instant"), Times.Once);
        _mockModelRotationService.Verify(x => x.RecordUsage("llama-3.1-8b-instant", 100), Times.Once);
        _mockAiLogService.Verify(x => x.EndApiCall(aiProcessLog1, false), Times.Once); 
        _mockAiLogService.Verify(x => x.EndApiCall(aiProcessLog2, true), Times.Once); 
        Assert.Equal("[{}]", result.ContentString);
        Assert.Equal(456, result.AILogId); 
    }

    [Fact]
    public async Task GenerateQuestions_WithServerError_Retries()
    {
        // Arrange
        var prompt = "Generate quiz questions";
        var modelConfig = new ModelConfig("llama-3.1-8b-instant", 1000, 10000, 100000, 1000000);

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
                                        content = "[{}]"
                                    }
                                }
                            },
                            usage = new { total_tokens = 100 }
                        }))
                    };
                }
            });

        var aiProcessLog1 = new AiProcessLog { Id = 123 };
        var aiProcessLog2 = new AiProcessLog { Id = 456 };
        _mockAiLogService.SetupSequence(x => x.StartApiCall(It.IsAny<AiApiCallStartDetail>()))
            .Returns(aiProcessLog1)
            .Returns(aiProcessLog2);

        _mockAiLogService.Setup(x => x.EndApiCall(aiProcessLog1, false)).Returns(Task.CompletedTask);
        _mockAiLogService.Setup(x => x.EndApiCall(aiProcessLog2, true)).Returns(Task.CompletedTask);

        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        Assert.Equal("[{}]", result.ContentString);
        Assert.Equal(456, result.AILogId); // Second log ID
        _mockAiLogService.Verify(x => x.EndApiCall(aiProcessLog1, false), Times.Once); 
        _mockAiLogService.Verify(x => x.EndApiCall(aiProcessLog2, true), Times.Once); 
    }

    [Fact]
    public async Task GenerateQuestions_WithEmptyResponse_Retries()
    {
        // Arrange
        var prompt = "Generate quiz questions";
        var modelConfig = new ModelConfig("llama-3.1-8b-instant", 1000, 10000, 100000, 1000000);

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
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(new
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
                        }))
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
                                        content = "[{\"question\": \"Test?\"}]"
                                    }
                                }
                            },
                            usage = new { total_tokens = 100 }
                        }))
                    };
                }
            });

        var aiProcessLog1 = new AiProcessLog { Id = 123 };
        var aiProcessLog2 = new AiProcessLog { Id = 456 };
        _mockAiLogService.SetupSequence(x => x.StartApiCall(It.IsAny<AiApiCallStartDetail>()))
            .Returns(aiProcessLog1)
            .Returns(aiProcessLog2);

        _mockAiLogService.Setup(x => x.EndApiCall(aiProcessLog1, false)).Returns(Task.CompletedTask);
        _mockAiLogService.Setup(x => x.EndApiCall(aiProcessLog2, true)).Returns(Task.CompletedTask);

        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        Assert.Equal("[{\"question\": \"Test?\"}]", result.ContentString);
        Assert.Equal(456, result.AILogId); // Second log ID
        _mockAiLogService.Verify(x => x.EndApiCall(aiProcessLog1, false), Times.Once); 
        _mockAiLogService.Verify(x => x.EndApiCall(aiProcessLog2, true), Times.Once); 
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

        var aiProcessLog = new AiProcessLog { Id = 123 };
        _mockAiLogService.Setup(x => x.StartApiCall(It.IsAny<AiApiCallStartDetail>())).Returns(aiProcessLog);
        _mockAiLogService.Setup(x => x.EndApiCall(aiProcessLog, true)).Returns(Task.CompletedTask);

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModel("llama-3.1-8b-instant");

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        Assert.Equal(expectedCleanResponse, result.ContentString);
        Assert.Equal(123, result.AILogId);
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

        var aiProcessLog = new AiProcessLog { Id = 123 };
        _mockAiLogService.Setup(x => x.StartApiCall(It.IsAny<AiApiCallStartDetail>())).Returns(aiProcessLog);
        _mockAiLogService.Setup(x => x.EndApiCall(aiProcessLog, true)).Returns(Task.CompletedTask);

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModel("llama-3.1-8b-instant");

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        var expected = JsonSerializer.Serialize(nestedResponse.questions);
        Assert.Equal(expected, result.ContentString);
        Assert.Equal(123, result.AILogId);
    }

    [Fact]
    public async Task GenerateQuestions_ExceedsMaxRetries_ThrowsException()
    {
        // Arrange
        var prompt = "Generate quiz questions";
        var modelConfig = new ModelConfig("llama-3.1-8b-instant", 1000, 10000, 100000, 1000000);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("Rate limit exceeded")
            });

        var aiProcessLogs = new List<AiProcessLog>
        {
            new AiProcessLog { Id = 1 },
            new AiProcessLog { Id = 2 },
            new AiProcessLog { Id = 3 },
            new AiProcessLog { Id = 4 },
            new AiProcessLog { Id = 5 }
        };

        var setupSequence = _mockAiLogService.SetupSequence(x => x.StartApiCall(It.IsAny<AiApiCallStartDetail>()));
        foreach (var log in aiProcessLogs)
        {
            setupSequence = setupSequence.Returns(log);
        }

        foreach (var log in aiProcessLogs)
        {
            _mockAiLogService.Setup(x => x.EndApiCall(log, false)).Returns(Task.CompletedTask);
        }

        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);
        _mockModelRotationService.Setup(x => x.HandleRateLimitExceeded(It.IsAny<string>()));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _groqService.GenerateQuesions(prompt));

        foreach (var log in aiProcessLogs)
        {
            _mockAiLogService.Verify(x => x.EndApiCall(log, false), Times.Once);
        }
    }

    [Fact]
    public async Task GenerateQuestions_WithNoAvailableModel_RetriesAfterDelay()
    {
        // Arrange
        var prompt = "Generate quiz questions";

        _mockModelRotationService.SetupSequence(x => x.GetAvailableModel())
            .Returns((ModelConfig)null)
            .Returns(new ModelConfig("llama-3.1-8b-instant", 1000, 10000, 100000, 1000000));

        var aiProcessLog = new AiProcessLog { Id = 123 };
        _mockAiLogService.Setup(x => x.StartApiCall(It.IsAny<AiApiCallStartDetail>())).Returns(aiProcessLog);
        _mockAiLogService.Setup(x => x.EndApiCall(aiProcessLog, true)).Returns(Task.CompletedTask);

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
        Assert.Equal("[{}]", result.ContentString);
        Assert.Equal(123, result.AILogId);
    }

    [Fact]
    public async Task GenerateQuestions_WithHttpRequestException_Retries()
    {
        // Arrange
        var prompt = "Generate quiz questions";
        var modelConfig = new ModelConfig("llama-3.1-8b-instant", 1000, 10000, 100000, 1000000);

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
                                    content = "[{}]"
                                }
                            }
                            },
                            usage = new { total_tokens = 100 }
                        }))
                    };
                }
            });

        var aiProcessLog1 = new AiProcessLog { Id = 123 };
        var aiProcessLog2 = new AiProcessLog { Id = 456 };
        _mockAiLogService.SetupSequence(x => x.StartApiCall(It.IsAny<AiApiCallStartDetail>()))
            .Returns(aiProcessLog1)
            .Returns(aiProcessLog2);

        _mockAiLogService.Setup(x => x.EndApiCall(aiProcessLog2, true)).Returns(Task.CompletedTask);

        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        Assert.Equal("[{}]", result.ContentString);
        Assert.Equal(456, result.AILogId);

        _mockAiLogService.Verify(x => x.EndApiCall(aiProcessLog1, false), Times.Never);

        _mockAiLogService.Verify(x => x.EndApiCall(aiProcessLog2, true), Times.Once);
    }

    [Fact]
    public async Task GenerateQuestions_WithTaskCanceledException_Retries()
    {
        // Arrange
        var prompt = "Generate quiz questions";
        var modelConfig = new ModelConfig("llama-3.1-8b-instant", 1000, 10000, 100000, 1000000);

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
                                    content = "[{}]"
                                }
                            }
                            },
                            usage = new { total_tokens = 100 }
                        }))
                    };
                }
            });

        var aiProcessLog1 = new AiProcessLog { Id = 123 };
        var aiProcessLog2 = new AiProcessLog { Id = 456 };
        _mockAiLogService.SetupSequence(x => x.StartApiCall(It.IsAny<AiApiCallStartDetail>()))
            .Returns(aiProcessLog1)
            .Returns(aiProcessLog2);

        _mockAiLogService.Setup(x => x.EndApiCall(aiProcessLog2, true)).Returns(Task.CompletedTask);

        _mockModelRotationService.Setup(x => x.GetAvailableModel()).Returns(modelConfig);

        // Act
        var result = await _groqService.GenerateQuesions(prompt);

        // Assert
        Assert.Equal("[{}]", result.ContentString);
        Assert.Equal(456, result.AILogId);

        _mockAiLogService.Verify(x => x.EndApiCall(aiProcessLog1, false), Times.Never);

        _mockAiLogService.Verify(x => x.EndApiCall(aiProcessLog2, true), Times.Once);
    }
    [Fact]
    public async Task GenerateQuestions_WithInvalidJsonInResponse_ThrowsJsonException()
    {
        // Arrange
        var prompt = "Generate quiz questions";
        var invalidJson = "invalid json content";

        var apiResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = invalidJson
                    }
                }
            },
            usage = new { total_tokens = 100 }
        };

        var aiProcessLog = new AiProcessLog { Id = 123 };
        _mockAiLogService.Setup(x => x.StartApiCall(It.IsAny<AiApiCallStartDetail>())).Returns(aiProcessLog);

        SetupSuccessfulApiResponse(apiResponse);
        SetupAvailableModel("llama-3.1-8b-instant");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _groqService.GenerateQuesions(prompt));
        Assert.Contains("JSON", exception.Message); 

        _mockAiLogService.Verify(x => x.EndApiCall(It.IsAny<AiProcessLog>(), It.IsAny<bool>()), Times.Never);
    }

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

    [Fact]
    public void CleanJsonResponse_WithObjectContainingQuestionsArray_ExtractsArray()
    {
        // Arrange
        var objectWithQuestions = @"{
            ""questions"": [
                {""question"": ""Q1?""},
                {""question"": ""Q2?""}
            ]
        }";

        // Act
        var result = InvokePrivateMethod<string>("CleanJsonResponse", objectWithQuestions);

        // Assert
        var expected = @"[
                {""question"": ""Q1?""},
                {""question"": ""Q2?""}
            ]";
        Assert.Equal(expected, result);
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

    private void SetupAvailableModel(string modelName = "llama-3.1-8b-instant")
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