using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
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
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IGenericRepository<AiProcessLog>> _mockAiLogRepository;
    private readonly AiQuestionGenerationService _aiQuestionService;

    public AiQuestionGenerationServiceTests()
    {
        _mockGroqService = new Mock<IGroqService>();
        _mockValidatorService = new Mock<IGroqContentValidatorService>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
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
            _mockScopeFactory.Object
        );
    }

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
}