using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class AiQuestionGenerationServiceTests
{
    private readonly Mock<IGroqService> _mockGroqService;
    private readonly Mock<IGroqContentValidatorService> _mockValidatorService;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IFetchContentFromUrlService> _mockFetchContentFromUrlService;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IGenericRepository<AiProcessLog>> _mockAiLogRepository;
    private readonly AiQuestionGenerationService _aiQuestionService;
    private readonly Mock<ICommonService> _mockCommonService;
    public AiQuestionGenerationServiceTests()
    {
        _mockGroqService = new Mock<IGroqService>();
        _mockValidatorService = new Mock<IGroqContentValidatorService>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockFetchContentFromUrlService = new Mock<IFetchContentFromUrlService>();
        _mockCommonService = new Mock<ICommonService>();

        // Default service instance (for non-PDF tests)
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockAiLogRepository = new Mock<IGenericRepository<AiProcessLog>>();

        // Setup service scope factory chain
        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockServiceScope.Object);
        _mockServiceScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IGenericRepository<AiProcessLog>)))
            .Returns(_mockAiLogRepository.Object);

        _aiQuestionService = new AiQuestionGenerationService(
            _mockGroqService.Object,
            _mockValidatorService.Object,
            _mockScopeFactory.Object,
            _mockFetchContentFromUrlService.Object,
            _mockCommonService.Object
        );
    }
    private Mock<IFormFile> CreateMockFormFile(string fileName, string content)
    {
        var mockFile = new Mock<IFormFile>();
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(stream.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockFile.Setup(f => f.ContentType).Returns("application/pdf");

        return mockFile;
    }
    #region GenerateFromPromptAsync Tests

    [Fact]
    public async Task GenerateFromPromptAsync_WithValidInput_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions about physics",
            Category = "educational",
            CategoryId = 1,
            QuestionSpec = new List<QuestionGenerationFormatDto>
            {
                new QuestionGenerationFormatDto
                {
                    QuestionDifficultyId = 1,
                    QuestionDifficultyName = "Medium",
                    QuestionPerQuestionType = new List<QuestionPerQuestionType>
                    {
                        new QuestionPerQuestionType
                        {
                            QuestionPerQuestionTypeId = 1,
                            QuestionPerQuestionTypeName = "Multiple Choice",
                            NoOfQuesitons = 5
                        }
                    }
                }
            }
        };

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false,
        };

        var generatedQuestions = new List<QuizQuestionDto>
        {
            new QuizQuestionDto
            {
                QueText = "What is Newton's first law?",
                QueDifficultyName = "Medium",
                QueTypeName = "Multiple Choice",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "Law of Inertia" },
                    new QueOption { Key = "option", Value = "Law of Motion" },
                    new QueOption { Key = "option", Value = "Law of Gravity" },
                    new QueOption { Key = "option", Value = "Law of Force" },
                    new QueOption { Key = "answer", Value = "Law of Inertia" }
                },
            }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);
        var aiLogId = 123;

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync((jsonResponse, aiLogId));

        var aiLog = new AiProcessLog { Id = aiLogId };
        var aiLogs = new List<AiProcessLog> { aiLog }.AsQueryable();
        _mockAiLogRepository.Setup(x => x.GetQueryableInclude())
            .Returns(aiLogs);
        _mockAiLogRepository.Setup(x => x.UpdateAsync(It.IsAny<AiProcessLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(Constants.QUESTION_GENERATION_SUCCESS, result.Message);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);

        var question = result.Data[0];
        Assert.Equal(1, question.CategoryId);
        Assert.Equal("educational", question.CategoryName);
        Assert.Equal(1, question.QueDifficultyId);
        Assert.Equal("Medium", question.QueDifficultyName);
        Assert.Equal(1, question.QueTypeId);
        Assert.Equal("Multiple Choice", question.QueTypeName);

        Assert.Equal(1, result.Count);
        Assert.NotNull(result.Validation);
        Assert.Equal("educational", result.Validation.Category);

        _mockValidatorService.Verify(x => x.ValidateContentAsync(request.Prompt, request.Category), Times.Once);
        _mockGroqService.Verify(x => x.GenerateQuesions(It.IsAny<string>()), Times.Once);
        _mockAiLogRepository.Verify(x => x.UpdateAsync(It.IsAny<AiProcessLog>()), Times.Once);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithValidationFailure_ReturnsErrorResponse()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Inappropriate content",
            Category = "educational"
        };

        var validationResult = new ContentValidationResult
        {
            IsValid = false,
            IsMatch = false,
            Category = "inappropriate",
            Reason = "Content contains inappropriate material",
            ValidationFailed = true,
        };

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(string.Format(Constants.CONTENT_VALIDATION_FAILED, validationResult.Reason), result.Message);
        Assert.Equal(400, result.StatusCode);
        Assert.NotNull(result.Validation);
        Assert.False(result.Validation.ShouldProceed);
        Assert.Equal("Content contains inappropriate material", result.Validation.Reason);
        Assert.Equal("inappropriate", result.Validation.Category);
        _mockGroqService.Verify(x => x.GenerateQuesions(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithNullQuestionSpecs_UsesDefaultSpecs()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions",
            Category = "educational",
            QuestionSpec = null!
        };

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false,
        };

        var generatedQuestions = new List<QuizQuestionDto>
        {
            new QuizQuestionDto {
                QueText = "Test question?",
                QueTypeName = "Multiple Choice",
                QueDifficultyName = "Medium",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "A" },
                    new QueOption { Key = "option", Value = "B" },
                    new QueOption { Key = "option", Value = "C" },
                    new QueOption { Key = "answer", Value = "A" }
                }
             }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);
        var aiLogId = 123;

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync((jsonResponse, aiLogId));

        // Mock without AsNoTracking()
        var emptyAiLogs = new List<AiProcessLog>().AsQueryable();
        _mockAiLogRepository.Setup(x => x.GetQueryableInclude())
            .Returns(emptyAiLogs);
        _mockAiLogRepository.Setup(x => x.UpdateAsync(It.IsAny<AiProcessLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Specifications);
        Assert.Single(result.Specifications);
        Assert.Equal("Multiple Choice", result.Specifications[0].Type);
        Assert.Equal("Medium", result.Specifications[0].Difficulty);
        Assert.Equal(10, result.Specifications[0].Count);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithComplexQuestionSpecs_FlattensAndMapsCorrectly()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions",
            Category = "educational",
            CategoryId = 2,
            QuestionSpec = new List<QuestionGenerationFormatDto>
            {
                new QuestionGenerationFormatDto
                {
                    QuestionDifficultyId = 1,
                    QuestionDifficultyName = "Easy",
                    QuestionPerQuestionType = new List<QuestionPerQuestionType>
                    {
                        new QuestionPerQuestionType
                        {
                            QuestionPerQuestionTypeId = 1,
                            QuestionPerQuestionTypeName = "Multiple Choice",
                            NoOfQuesitons = 5
                        },
                        new QuestionPerQuestionType
                        {
                            QuestionPerQuestionTypeId = 2,
                            QuestionPerQuestionTypeName = "True/False",
                            NoOfQuesitons = 3
                        }
                    }
                },
                new QuestionGenerationFormatDto
                {
                    QuestionDifficultyId = 3,
                    QuestionDifficultyName = "Hard",
                    QuestionPerQuestionType = new List<QuestionPerQuestionType>
                    {
                        new QuestionPerQuestionType
                        {
                            QuestionPerQuestionTypeId = 3,
                            QuestionPerQuestionTypeName = "Fill in the Blank",
                            NoOfQuesitons = 2
                        }
                    }
                }
            }
        };

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false,
        };

        var generatedQuestions = new List<QuizQuestionDto>
        {
            new QuizQuestionDto {
                QueText = "Easy MCQ question?",
                QueTypeName = "Multiple Choice",
                QueDifficultyName = "Easy",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "A" },
                    new QueOption { Key = "option", Value = "B" },
                    new QueOption { Key = "answer", Value = "A" }
                }
             },
            new QuizQuestionDto {
                QueText = "Hard Fill question?",
                QueTypeName = "Fill in the Blank",
                QueDifficultyName = "Hard",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "answer", Value = "Gravity" }
                }
             }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);
        var aiLogId = 123;

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync((jsonResponse, aiLogId));

        var emptyAiLogs = new List<AiProcessLog>().AsQueryable();
        _mockAiLogRepository.Setup(x => x.GetQueryableInclude())
            .Returns(emptyAiLogs);
        _mockAiLogRepository.Setup(x => x.UpdateAsync(It.IsAny<AiProcessLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Specifications);
        Assert.Equal(3, result.Specifications.Count);

        // Verify flattened specifications
        var easyMultipleChoice = result.Specifications.First(s => s.Type == "Multiple Choice" && s.Difficulty == "Easy");
        Assert.Equal(5, easyMultipleChoice.Count);

        var easyTrueFalse = result.Specifications.First(s => s.Type == "True/False" && s.Difficulty == "Easy");
        Assert.Equal(3, easyTrueFalse.Count);

        var hardFillBlank = result.Specifications.First(s => s.Type == "Fill in the Blank" && s.Difficulty == "Hard");
        Assert.Equal(2, hardFillBlank.Count);

        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);

        var easyQuestion = result.Data.First(q => q.QueDifficultyName == "Easy");
        Assert.Equal(2, easyQuestion.CategoryId);
        Assert.Equal("educational", easyQuestion.CategoryName);
        Assert.Equal(1, easyQuestion.QueDifficultyId);
        Assert.Equal(1, easyQuestion.QueTypeId);

        var hardQuestion = result.Data.First(q => q.QueDifficultyName == "Hard");
        Assert.Equal(2, hardQuestion.CategoryId);
        Assert.Equal("educational", hardQuestion.CategoryName);
        Assert.Equal(3, hardQuestion.QueDifficultyId);
        Assert.Equal(3, hardQuestion.QueTypeId);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithEmptyGroqResponse_ReturnsErrorResponse()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions",
            Category = "educational",
            QuestionSpec = new List<QuestionGenerationFormatDto>
            {
                new QuestionGenerationFormatDto
                {
                    QuestionDifficultyName = "Medium",
                    QuestionPerQuestionType = new List<QuestionPerQuestionType>
                    {
                        new QuestionPerQuestionType
                        {
                            QuestionPerQuestionTypeName = "Multiple Choice",
                            NoOfQuesitons = 5
                        }
                    }
                }
            }
        };

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false,
        };

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(("", 0));

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(Constants.FAILED_TO_GENERATE_QUESTIONS, result.Message);
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithInvalidJsonResponse_ReturnsErrorResponse()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions",
            Category = "educational",
            QuestionSpec = new List<QuestionGenerationFormatDto>
            {
                new QuestionGenerationFormatDto
                {
                    QuestionDifficultyName = "Medium",
                    QuestionPerQuestionType = new List<QuestionPerQuestionType>
                    {
                        new QuestionPerQuestionType
                        {
                            QuestionPerQuestionTypeName = "Multiple Choice",
                            NoOfQuesitons = 5
                        }
                    }
                }
            }
        };

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false,
        };

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(("Invalid JSON {", 0));

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(Constants.AI_RESPONSE_FORMAT_ERROR, result.Message);
        Assert.NotNull(result.Error);
        Assert.Equal("Invalid JSON {", result.RawResponse);
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithException_ReturnsErrorResponse()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions",
            Category = "educational"
        };

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ThrowsAsync(new Exception("Validation service error"));

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(Constants.UNEXPECTED_ERROR_GENERATING_QUESTIONS, result.Message);
        Assert.Equal("Validation service error", result.Error);
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithZeroQuestionCount_ExcludesFromResult()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions",
            Category = "educational",
            QuestionSpec = new List<QuestionGenerationFormatDto>
            {
                new QuestionGenerationFormatDto
                {
                    QuestionDifficultyName = "Easy",
                    QuestionPerQuestionType = new List<QuestionPerQuestionType>
                    {
                        new QuestionPerQuestionType
                        {
                            QuestionPerQuestionTypeName = "MCQ",
                            NoOfQuesitons = 5
                        },
                        new QuestionPerQuestionType
                        {
                            QuestionPerQuestionTypeName = "TF",
                            NoOfQuesitons = 0
                        }
                    }
                }
            }
        };

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false,
        };

        var generatedQuestions = new List<QuizQuestionDto>
        {
            new QuizQuestionDto {
                QueText = "Test question?",
                QueTypeName = "MCQ",
                QueDifficultyName = "Easy",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "A" },
                    new QueOption { Key = "answer", Value = "A" }
                }
             }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);
        var aiLogId = 123;

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync((jsonResponse, aiLogId));

        var emptyAiLogs = new List<AiProcessLog>().AsQueryable();
        _mockAiLogRepository.Setup(x => x.GetQueryableInclude())
            .Returns(emptyAiLogs);
        _mockAiLogRepository.Setup(x => x.UpdateAsync(It.IsAny<AiProcessLog>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Specifications);
        Assert.Single(result.Specifications);
        Assert.Equal("MCQ", result.Specifications[0].Type);
        Assert.Equal(5, result.Specifications[0].Count);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_UpdatesAiLogWithQuestionCount()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions",
            Category = "educational",
            QuestionSpec = new List<QuestionGenerationFormatDto>
            {
                new QuestionGenerationFormatDto
                {
                    QuestionDifficultyName = "Medium",
                    QuestionPerQuestionType = new List<QuestionPerQuestionType>
                    {
                        new QuestionPerQuestionType
                        {
                            QuestionPerQuestionTypeName = "Multiple Choice",
                            NoOfQuesitons = 3
                        }
                    }
                }
            }
        };

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false,
        };

        var generatedQuestions = new List<QuizQuestionDto>
        {
            new QuizQuestionDto {
                QueText = "Question 1?",
                QueTypeName = "Multiple Choice",
                QueDifficultyName = "Medium",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "A" },
                    new QueOption { Key = "answer", Value = "A" }
                }
             },
            new QuizQuestionDto {
                QueText = "Question 2?",
                QueTypeName = "Multiple Choice",
                QueDifficultyName = "Medium",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "B" },
                    new QueOption { Key = "answer", Value = "B" }
                }
             }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);
        var aiLogId = 123;

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync((jsonResponse, aiLogId));

        var aiLog = new AiProcessLog { Id = aiLogId };
        var aiLogs = new List<AiProcessLog> { aiLog }.AsQueryable();
        _mockAiLogRepository.Setup(x => x.GetQueryableInclude())
            .Returns(aiLogs);
        _mockAiLogRepository.Setup(x => x.UpdateAsync(It.IsAny<AiProcessLog>()))
            .Returns(Task.CompletedTask)
            .Callback<AiProcessLog>(log =>
            {
                Assert.Equal(aiLogId, log.Id);
                Assert.NotNull(log.ExtraInfo);
                Assert.Contains(Constants.GENERATED_QUESTIONS_COUNT_JSON_KEY, log.ExtraInfo);
            });

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.True(result.Success);
        _mockAiLogRepository.Verify(x => x.UpdateAsync(It.IsAny<AiProcessLog>()), Times.Once);
    }

    #endregion

    #region GenerateQuestionUsingWebURL Tests

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithValidUrl_ReturnsSuccessResponse()
    {
        // Arrange
        var webRequest = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "https://example.com/physics",
            QuestionSpec = new List<QuestionGenerationFormatDto>
            {
                new QuestionGenerationFormatDto
                {
                    QuestionDifficultyName = "Medium",
                    QuestionPerQuestionType = new List<QuestionPerQuestionType>
                    {
                        new QuestionPerQuestionType
                        {
                            QuestionPerQuestionTypeName = "Multiple Choice",
                            NoOfQuesitons = 2
                        }
                    }
                }
            },
            Category = "educational",
            CategoryId = 5
        };

        const string webContent = "Physics is the study of matter and energy...";

        var validationResult = new ContentValidationResult
        {
            Reason = "Valid web content",
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            ValidationFailed = false,
        };

        var generatedQuestions = new List<QuizQuestionDto>
        {
            new QuizQuestionDto { QueText = "What is physics?", QueTypeName = "Multiple Choice", QueDifficultyName = "Medium" }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);
        var aiLogId = 999;

        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(webRequest.Url!))
            .ReturnsAsync(webContent);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(webContent, webRequest.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync((jsonResponse, aiLogId));

        var aiLogs = new List<AiProcessLog> { new AiProcessLog { Id = aiLogId } }.AsQueryable();
        _mockAiLogRepository.Setup(x => x.GetQueryableInclude()).Returns(aiLogs);
        _mockAiLogRepository.Setup(x => x.UpdateAsync(It.IsAny<AiProcessLog>())).Returns(Task.CompletedTask);

        // Act
        var result = await _aiQuestionService.GenerateQuestionUsingWebURL(webRequest);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(Constants.QUESTION_GENERATION_SUCCESS, result.Message);
        Assert.Single(result.Data);
        Assert.Equal(5, result.Data[0].CategoryId);
        Assert.Equal("educational", result.Data[0].CategoryName);

        _mockFetchContentFromUrlService.Verify(x => x.FetchAndValidateAsync(webRequest.Url!), Times.Once);
        _mockValidatorService.Verify(x => x.ValidateContentAsync(webContent, webRequest.Category), Times.Once);
        _mockGroqService.Verify(x => x.GenerateQuesions(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithInvalidUrl_ThrowsAppException()
    {
        // Arrange
        var webRequest = new GenerateQuestionUsingWebRequestDTO
        {
            Url = null
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(
            () => _aiQuestionService.GenerateQuestionUsingWebURL(webRequest));

        Assert.Equal(Constants.INVALID_URL_PROVIDED, ex.Message);
        Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithEmptyContent_ThrowsAppException()
    {
        // Arrange
        var webRequest = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "https://example.com/empty"
        };

        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(webRequest.Url!))
            .ReturnsAsync(string.Empty);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(
            async () => await _aiQuestionService.GenerateQuestionUsingWebURL(webRequest)
        );

        Assert.Equal("The website is safe.", exception.Message);
        _mockFetchContentFromUrlService.Verify(x => x.FetchAndValidateAsync(webRequest.Url), Times.Once);
    }

    #endregion


    #region GenerateFromPdfAsync Tests
    [Fact]
    public async Task GenerateFromPdfAsync_WithValidPdf_ReturnsSuccessResponse()
    {
        // Arrange
        var mockFile = CreateMockFormFile("sample.pdf", "PDF content");

        var request = new GenerateQuizFromPDFRequest
        {
            Prompt = mockFile.Object,
            Category = "educational",
            CategoryId = 1
        };

        var extractedText = "Newton's first law states that an object at rest stays at rest.";

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false
        };

        var generatedQuestions = new List<QuizQuestionDto>
        {
            new QuizQuestionDto
            {
                QueText = "What is Newton's first law?",
                QueDifficultyName = "Medium",
                QueTypeName = "Multiple Choice",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "Law of Inertia" },
                    new QueOption { Key = "option", Value = "Law of Motion" },
                    new QueOption { Key = "answer", Value = "Law of Inertia" }
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);

        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(extractedText);
        var aiLogId = 999;

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(extractedText, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync((jsonResponse, aiLogId));

        // Act
        var result = await _aiQuestionService.GenerateFromPdfAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Questions generated successfully.", result.Message);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        _mockCommonService.Verify(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()), Times.Once);
        _mockValidatorService.Verify(x => x.ValidateContentAsync(extractedText, request.Category), Times.Once);
        _mockGroqService.Verify(x => x.GenerateQuesions(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GenerateFromPdfAsync_WithEmptyExtractedText_ThrowsAppException()
    {
        // Arrange
        var mockFile = CreateMockFormFile("empty.pdf", "");

        var request = new GenerateQuizFromPDFRequest
        {
            Prompt = mockFile.Object,
            Category = "educational"
        };

        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("");

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(
            () => _aiQuestionService.GenerateFromPdfAsync(request)
        );

        _mockValidatorService.Verify(x => x.ValidateContentAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockGroqService.Verify(x => x.GenerateQuesions(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GenerateFromPdfAsync_WithWhitespaceExtractedText_ThrowsAppException()
    {
        // Arrange
        var mockFile = CreateMockFormFile("whitespace.pdf", "   ");

        var request = new GenerateQuizFromPDFRequest
        {
            Prompt = mockFile.Object,
            Category = "educational"
        };

        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("   \n\t   ");

        // Act & Assert
        await Assert.ThrowsAsync<AppException>(
            () => _aiQuestionService.GenerateFromPdfAsync(request)
        );
    }

    [Fact]
    public async Task GenerateFromPdfAsync_WithValidQuestionSpec_ParsesAndUsesSpec()
    {
        // Arrange
        var mockFile = CreateMockFormFile("sample.pdf", "PDF content");

        var questionSpec = new List<QuestionGenerationFormatDto>
        {
            new QuestionGenerationFormatDto
            {
                QuestionDifficultyName = "Easy",
                QuestionDifficultyId = 1,
                QuestionPerQuestionType = new List<QuestionPerQuestionType>
                {
                    new QuestionPerQuestionType
                    {
                        QuestionPerQuestionTypeName = "Multiple Choice",
                        QuestionPerQuestionTypeId = 1,
                        NoOfQuesitons = 5
                    }
                }
            }
        };

        var request = new GenerateQuizFromPDFRequest
        {
            Prompt = mockFile.Object,
            Category = "educational",
            CategoryId = 1,
            QuestionSpec = JsonSerializer.Serialize(questionSpec)
        };

        var extractedText = "Sample PDF content about science.";

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false
        };

        var generatedQuestions = new List<QuizQuestionDto>
        {
            new QuizQuestionDto
            {
                QueText = "Test question?",
                QueDifficultyName = "Easy",
                QueTypeName = "Multiple Choice",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "A" },
                    new QueOption { Key = "answer", Value = "A" }
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);
        var aiLogId = 999;
        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(extractedText);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(extractedText, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync((jsonResponse, aiLogId));

        // Act
        var result = await _aiQuestionService.GenerateFromPdfAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Specifications);
        Assert.Single(result.Specifications);
        Assert.Equal("Multiple Choice", result.Specifications[0].Type);
        Assert.Equal("Easy", result.Specifications[0].Difficulty);
        Assert.Equal(5, result.Specifications[0].Count);
    }

    [Fact]
    public async Task GenerateFromPdfAsync_WithInvalidQuestionSpecJson_ThrowsAppException()
    {
        // Arrange
        var mockFile = CreateMockFormFile("sample.pdf", "PDF content");

        var request = new GenerateQuizFromPDFRequest
        {
            Prompt = mockFile.Object,
            Category = "educational",
            QuestionSpec = "{ invalid json }"
        };

        var extractedText = "Sample PDF content.";

        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(extractedText);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(
            () => _aiQuestionService.GenerateFromPdfAsync(request)
        );

        Assert.Equal("Invalid or missing question specifications.", exception.Message);
    }

    [Fact]
    public async Task GenerateFromPdfAsync_WithNullQuestionSpec_UsesDefaultSpec()
    {
        // Arrange
        var mockFile = CreateMockFormFile("sample.pdf", "PDF content");

        var request = new GenerateQuizFromPDFRequest
        {
            Prompt = mockFile.Object,
            Category = "educational",
            QuestionSpec = null
        };

        var extractedText = "Sample PDF content about science.";

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false
        };

        var generatedQuestions = new List<QuizQuestionDto>
        {
            new QuizQuestionDto
            {
                QueText = "Test question?",
                QueDifficultyName = "Medium",
                QueTypeName = "Multiple Choice",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "A" },
                    new QueOption { Key = "answer", Value = "A" }
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);
        var aiLogId = 999;
        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(extractedText);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(extractedText, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync((jsonResponse, aiLogId));

        // Act
        var result = await _aiQuestionService.GenerateFromPdfAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Specifications);
        Assert.Single(result.Specifications);
        Assert.Equal("Multiple Choice", result.Specifications[0].Type);
        Assert.Equal("Medium", result.Specifications[0].Difficulty);
        Assert.Equal(10, result.Specifications[0].Count);
    }

    [Fact]
    public async Task GenerateFromPdfAsync_WithEmptyStringQuestionSpec_UsesDefaultSpec()
    {
        // Arrange
        var mockFile = CreateMockFormFile("sample.pdf", "PDF content");

        var request = new GenerateQuizFromPDFRequest
        {
            Prompt = mockFile.Object,
            Category = "educational",
            QuestionSpec = ""
        };

        var extractedText = "Sample PDF content about science.";

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false
        };

        var generatedQuestions = new List<QuizQuestionDto>
        {
            new QuizQuestionDto
            {
                QueText = "Test question?",
                QueDifficultyName = "Medium",
                QueTypeName = "Multiple Choice",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "A" },
                    new QueOption { Key = "answer", Value = "A" }
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);
        var aiLogId = 999;
        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(extractedText);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(extractedText, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync((jsonResponse, aiLogId));

        // Act
        var result = await _aiQuestionService.GenerateFromPdfAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Specifications);
        Assert.Single(result.Specifications);
        Assert.Equal("Multiple Choice", result.Specifications[0].Type);
        Assert.Equal("Medium", result.Specifications[0].Difficulty);
        Assert.Equal(10, result.Specifications[0].Count);
    }

    [Fact]
    public async Task GenerateFromPdfAsync_WithValidationFailure_ReturnsErrorResponse()
    {
        // Arrange
        var mockFile = CreateMockFormFile("inappropriate.pdf", "Bad content");

        var request = new GenerateQuizFromPDFRequest
        {
            Prompt = mockFile.Object,
            Category = "educational"
        };

        var extractedText = "Inappropriate content from PDF.";

        var validationResult = new ContentValidationResult
        {
            IsValid = false,
            IsMatch = false,
            Category = "inappropriate",
            Reason = "Content contains inappropriate material",
            ValidationFailed = true
        };

        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(extractedText);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(extractedText, request.Category))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _aiQuestionService.GenerateFromPdfAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Content validation failed: Content contains inappropriate material", result.Message);
        Assert.Equal(400, result.StatusCode);
        _mockGroqService.Verify(x => x.GenerateQuesions(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GenerateFromPdfAsync_WithPdfExtractionException_ThrowsException()
    {
        // Arrange
        var mockFile = CreateMockFormFile("corrupted.pdf", "Bad data");

        var request = new GenerateQuizFromPDFRequest
        {
            Prompt = mockFile.Object,
            Category = "educational"
        };

        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ThrowsAsync(new Exception("PDF extraction failed"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _aiQuestionService.GenerateFromPdfAsync(request)
        );

        Assert.Equal("PDF extraction failed", exception.Message);
    }

    [Fact]
    public async Task GenerateFromPdfAsync_WithCategoryIdAndCategory_MapsCategoryToQuestions()
    {
        // Arrange
        var mockFile = CreateMockFormFile("sample.pdf", "PDF content");

        var request = new GenerateQuizFromPDFRequest
        {
            Prompt = mockFile.Object,
            Category = "Science",
            CategoryId = 5
        };

        var extractedText = "Science content from PDF.";

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "Science",
            Reason = "Valid content",
            ValidationFailed = false
        };

        var generatedQuestions = new List<QuizQuestionDto>
        {
            new QuizQuestionDto
            {
                QueText = "Test question?",
                QueDifficultyName = "Medium",
                QueTypeName = "Multiple Choice",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "A" },
                    new QueOption { Key = "answer", Value = "A" }
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);
        var aiLogId = 999;
        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(extractedText);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(extractedText, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync((jsonResponse, aiLogId));

        // Act
        var result = await _aiQuestionService.GenerateFromPdfAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal(5, result.Data[0].CategoryId);
        Assert.Equal("Science", result.Data[0].CategoryName);
    }

    [Fact]
    public async Task GenerateFromPdfAsync_WithComplexQuestionSpec_ParsesAndFlattensCorrectly()
    {
        // Arrange
        var mockFile = CreateMockFormFile("sample.pdf", "PDF content");

        var questionSpec = new List<QuestionGenerationFormatDto>
        {
            new QuestionGenerationFormatDto
            {
                QuestionDifficultyName = "Easy",
                QuestionDifficultyId = 1,
                QuestionPerQuestionType = new List<QuestionPerQuestionType>
                {
                    new QuestionPerQuestionType
                    {
                        QuestionPerQuestionTypeName = "Multiple Choice",
                        QuestionPerQuestionTypeId = 1,
                        NoOfQuesitons = 3
                    },
                    new QuestionPerQuestionType
                    {
                        QuestionPerQuestionTypeName = "True/False",
                        QuestionPerQuestionTypeId = 2,
                        NoOfQuesitons = 2
                    }
                }
            },
            new QuestionGenerationFormatDto
            {
                QuestionDifficultyName = "Hard",
                QuestionDifficultyId = 3,
                QuestionPerQuestionType = new List<QuestionPerQuestionType>
                {
                    new QuestionPerQuestionType
                    {
                        QuestionPerQuestionTypeName = "Fill in the Blank",
                        QuestionPerQuestionTypeId = 3,
                        NoOfQuesitons = 1
                    }
                }
            }
        };

        var request = new GenerateQuizFromPDFRequest
        {
            Prompt = mockFile.Object,
            Category = "educational",
            QuestionSpec = JsonSerializer.Serialize(questionSpec)
        };

        var extractedText = "Sample PDF content.";

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false
        };

        var generatedQuestions = new List<QuizQuestionDto>
        {
            new QuizQuestionDto
            {
                QueText = "Test?",
                QueDifficultyName = "Easy",
                QueTypeName = "Multiple Choice",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "answer", Value = "A" }
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);
        var aiLogId = 999;
        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(extractedText);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(extractedText, request.Category))
            .ReturnsAsync(validationResult);
        _mockGroqService
                    .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
                    .ReturnsAsync((jsonResponse, aiLogId));

        // Act
        var result = await _aiQuestionService.GenerateFromPdfAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Specifications);
        Assert.Equal(3, result.Specifications.Count);

        var easyMcq = result.Specifications.First(s => s.Type == "Multiple Choice");
        Assert.Equal("Easy", easyMcq.Difficulty);
        Assert.Equal(3, easyMcq.Count);

        var easyTf = result.Specifications.First(s => s.Type == "True/False");
        Assert.Equal("Easy", easyTf.Difficulty);
        Assert.Equal(2, easyTf.Count);

        var hardFib = result.Specifications.First(s => s.Type == "Fill in the Blank");
        Assert.Equal("Hard", hardFib.Difficulty);
        Assert.Equal(1, hardFib.Count);
    }

    #endregion
}