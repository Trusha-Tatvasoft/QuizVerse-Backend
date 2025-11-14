using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class AiQuestionGenerationServiceTests
{
    private readonly Mock<IGroqService> _mockGroqService;
    private readonly Mock<IGroqContentValidatorService> _mockValidatorService;
    private readonly Mock<IFetchContentFromUrlService> _mockFetchContentFromUrlService;
    private readonly AiQuestionGenerationService _aiQuestionService;
    private readonly Mock<ICommonService> _mockCommonService;
    public AiQuestionGenerationServiceTests()
    {
        _mockGroqService = new Mock<IGroqService>();
        _mockValidatorService = new Mock<IGroqContentValidatorService>();
        _mockFetchContentFromUrlService = new Mock<IFetchContentFromUrlService>();
        _mockCommonService = new Mock<ICommonService>();

        // Default service instance (for non-PDF tests)
        _aiQuestionService = new AiQuestionGenerationService(
            _mockGroqService.Object,
            _mockValidatorService.Object,
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
            CategoryId = 1
        };

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
                    new QueOption { Key = "option", Value = "Law of Gravity" },
                    new QueOption { Key = "option", Value = "Law of Force" },
                    new QueOption { Key = "answer", Value = "Law of Inertia" }
                },
            }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Questions generated successfully.", result.Message);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal(1, result.Count);
        Assert.NotNull(result.Validation);
        Assert.Equal("educational", result.Validation.Category);
        Assert.Equal(1, result.Data[0].CategoryId);
        Assert.Equal("educational", result.Data[0].CategoryName);
        _mockValidatorService.Verify(x => x.ValidateContentAsync(request.Prompt, request.Category), Times.Once);
        _mockGroqService.Verify(x => x.GenerateQuesions(It.IsAny<string>()), Times.Once);
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
            ValidationFailed = true
        };

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Content validation failed: Content contains inappropriate material", result.Message);
        Assert.Equal(400, result.StatusCode);
        Assert.NotNull(result.Validation);
        Assert.False(result.Validation.ShouldProceed);
        Assert.Equal("Content contains inappropriate material", result.Validation.Reason);
        Assert.Equal("inappropriate", result.Validation.Category);
        _mockGroqService.Verify(x => x.GenerateQuesions(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithEmptyPrompt_ReturnsErrorResponse()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "",
            Category = "educational"
        };

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false
        };

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed to generate questions.", result.Message);
        Assert.Equal(500, result.StatusCode);
        _mockGroqService.Verify(x => x.GenerateQuesions(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithNullQuestionSpecs_UsesDefaultSpecs()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions",
            Category = "educational",
            QuestionSpec = null
        };

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

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

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
    public async Task GenerateFromPromptAsync_WithEmptyQuestionSpecs_UsesDefaultSpecs()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions",
            Category = "educational",
            QuestionSpec = new List<QuestionGenerationFormatDto>()
        };

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

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

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
    public async Task GenerateFromPromptAsync_WithComplexQuestionSpecs_FlattensCorrectly()
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
                    QuestionDifficultyId = 1,
                    QuestionPerQuestionType = new List<QuestionPerQuestionType>
                    {
                        new QuestionPerQuestionType { QuestionPerQuestionTypeName = "Multiple Choice", QuestionPerQuestionTypeId = 1, NoOfQuesitons = 5 },
                        new QuestionPerQuestionType { QuestionPerQuestionTypeName = "True/False", QuestionPerQuestionTypeId = 2, NoOfQuesitons = 3 }
                    }
                },
                new QuestionGenerationFormatDto
                {
                    QuestionDifficultyName = "Hard",
                    QuestionDifficultyId = 2,
                    QuestionPerQuestionType = new List<QuestionPerQuestionType>
                    {
                        new QuestionPerQuestionType { QuestionPerQuestionTypeName = "Fill in the Blank", QuestionPerQuestionTypeId = 3, NoOfQuesitons = 2 }
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
            ValidationFailed = false
        };

        var generatedQuestions = new List<QuizQuestionDto>
        {
            new QuizQuestionDto {
                QueText = "Test question?",
                QueTypeName = "Multiple Choice",
                QueDifficultyName = "Easy",
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

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Specifications);
        Assert.Equal(3, result.Specifications.Count);

        var easyMultipleChoice = result.Specifications.First(s => s.Type == "Multiple Choice" && s.Difficulty == "Easy");
        Assert.Equal(5, easyMultipleChoice.Count);

        var easyTrueFalse = result.Specifications.First(s => s.Type == "True/False" && s.Difficulty == "Easy");
        Assert.Equal(3, easyTrueFalse.Count);

        var hardFillBlank = result.Specifications.First(s => s.Type == "Fill in the Blank" && s.Difficulty == "Hard");
        Assert.Equal(2, hardFillBlank.Count);

        // Verify ID mapping
        Assert.Equal(1, result.Data[0].QueDifficultyId);
        Assert.Equal(1, result.Data[0].QueTypeId);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithEmptyGroqResponse_ReturnsErrorResponse()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions",
            Category = "educational"
        };

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false
        };

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync("");

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed to generate questions.", result.Message);
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithEmptyArrayGroqResponse_ReturnsErrorResponse()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions",
            Category = "educational"
        };

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false
        };

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync("[]");

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed to generate questions.", result.Message);
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithInvalidJsonResponse_ReturnsErrorResponse()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions",
            Category = "educational"
        };

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false
        };

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync("Invalid JSON {");

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("AI response format error.", result.Message);
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
        Assert.Equal("Unexpected error generating questions.", result.Message);
        Assert.Equal("Validation service error", result.Error);
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithNullCategory_UsesDefaultCategory()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions",
            Category = null
        };

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

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, It.IsAny<string>()))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.True(result.Success);
        _mockValidatorService.Verify(x => x.ValidateContentAsync(request.Prompt, "educational"), Times.Once);
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
                        new QuestionPerQuestionType { QuestionPerQuestionTypeName = "MCQ", NoOfQuesitons = 5 },
                        new QuestionPerQuestionType { QuestionPerQuestionTypeName = "TF", NoOfQuesitons = 0 }
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
            ValidationFailed = false
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

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

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
    public async Task GenerateFromPromptAsync_WithNullQuestionPerQuestionType_ReturnsErrorResponse()
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
                    QuestionPerQuestionType = null
                }
            }
        };

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false
        };

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid or missing question specifications.", result.Message);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithEmptyQuestionPerQuestionType_ReturnsErrorResponse()
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
                    QuestionPerQuestionType = new List<QuestionPerQuestionType>()
                }
            }
        };

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false
        };

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid or missing question specifications.", result.Message);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_MapsIdsCorrectly_WhenMatchingDifficultyAndType()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions",
            Category = "Physics",
            CategoryId = 5,
            QuestionSpec = new List<QuestionGenerationFormatDto>
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
                            QuestionPerQuestionTypeId = 10,
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
            ValidationFailed = false
        };

        var generatedQuestions = new List<QuizQuestionDto>
        {
            new QuizQuestionDto {
                QueText = "Test question?",
                QueTypeName = "Multiple Choice",
                QueDifficultyName = "Easy",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "A" },
                    new QueOption { Key = "answer", Value = "A" }
                }
             }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(5, result.Data[0].CategoryId);
        Assert.Equal("Physics", result.Data[0].CategoryName);
        Assert.Equal(1, result.Data[0].QueDifficultyId);
        Assert.Equal("Easy", result.Data[0].QueDifficultyName);
        Assert.Equal(10, result.Data[0].QueTypeId);
        Assert.Equal("Multiple Choice", result.Data[0].QueTypeName);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_DoesNotMapIds_WhenNoMatchingSpecs()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions",
            Category = "Physics",
            CategoryId = 5,
            QuestionSpec = null
        };

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
            new QuizQuestionDto {
                QueText = "Test question?",
                QueTypeName = "Multiple Choice",
                QueDifficultyName = "Easy",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "A" },
                    new QueOption { Key = "answer", Value = "A" }
                }
             }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(request.Prompt, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await _aiQuestionService.GenerateFromPromptAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(5, result.Data[0].CategoryId);
        Assert.Equal("Physics", result.Data[0].CategoryName);
    }

    #endregion

    #region GenerateQuestionUsingWebURL Tests

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithValidUrl_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "https://example.com/article",
            Category = "Science",
            CategoryId = 2,
            QuestionSpec = new List<QuestionGenerationFormatDto>
            {
                new QuestionGenerationFormatDto
                {
                    QuestionDifficultyName = "Medium",
                    QuestionDifficultyId = 2,
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
            }
        };

        var webContent = "This is an article about photosynthesis and how plants convert sunlight into energy.";

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
            new QuizQuestionDto {
                QueText = "What is photosynthesis?",
                QueTypeName = "Multiple Choice",
                QueDifficultyName = "Medium",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "Process of energy conversion" },
                    new QueOption { Key = "option", Value = "Plant respiration" },
                    new QueOption { Key = "option", Value = "Cell division" },
                    new QueOption { Key = "answer", Value = "Process of energy conversion" }
                }
             }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);

        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(request.Url))
            .ReturnsAsync(webContent);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(webContent, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await _aiQuestionService.GenerateQuestionUsingWebURL(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Questions generated successfully.", result.Message);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal(2, result.Data[0].CategoryId);
        Assert.Equal("Science", result.Data[0].CategoryName);
        _mockFetchContentFromUrlService.Verify(x => x.FetchAndValidateAsync(request.Url), Times.Once);
        _mockValidatorService.Verify(x => x.ValidateContentAsync(webContent, request.Category), Times.Once);
        _mockGroqService.Verify(x => x.GenerateQuesions(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithWhitespaceUrl_ThrowsAppException()
    {
        // Arrange
        var request = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "   ",
            Category = "Science"
        };

        // The FetchAndValidateAsync will be called and may throw its own exception
        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(It.IsAny<string>()))
            .ThrowsAsync(new AppException("The website is safe.", StatusCodes.Status400BadRequest));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(
            async () => await _aiQuestionService.GenerateQuestionUsingWebURL(request)
        );

        // The actual error message from FetchAndValidateAsync
        Assert.Equal("The website is safe.", exception.Message);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithEmptyContent_ThrowsAppException()
    {
        // Arrange
        var request = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "https://example.com/article",
            Category = "Science"
        };

        // FetchAndValidateAsync throws exception when content validation fails
        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(request.Url))
            .ThrowsAsync(new AppException("The website is safe.", StatusCodes.Status400BadRequest));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(
            async () => await _aiQuestionService.GenerateQuestionUsingWebURL(request)
        );

        Assert.Equal("The website is safe.", exception.Message);
        _mockFetchContentFromUrlService.Verify(x => x.FetchAndValidateAsync(request.Url), Times.Once);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithNullContent_ThrowsAppException()
    {
        // Arrange
        var request = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "https://example.com/article",
            Category = "Science"
        };

        // FetchAndValidateAsync throws exception when content is null/invalid
        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(request.Url))
            .ThrowsAsync(new AppException("The website is safe.", StatusCodes.Status400BadRequest));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(
            async () => await _aiQuestionService.GenerateQuestionUsingWebURL(request)
        );

        Assert.Equal("The website is safe.", exception.Message);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithInvalidUrl_ThrowsAppException()
    {
        // Arrange
        var request = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "not-a-valid-url",
            Category = "Science"
        };

        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(request.Url))
            .ThrowsAsync(new AppException("Invalid URL format", StatusCodes.Status400BadRequest));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(
            async () => await _aiQuestionService.GenerateQuestionUsingWebURL(request)
        );

        Assert.Equal("Invalid URL format", exception.Message);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithUnsafeWebsite_ThrowsAppException()
    {
        // Arrange
        var request = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "https://malicious-site.com",
            Category = "Science"
        };

        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(request.Url))
            .ThrowsAsync(new AppException("The website is not safe.", StatusCodes.Status400BadRequest));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(
            async () => await _aiQuestionService.GenerateQuestionUsingWebURL(request)
        );

        Assert.Equal("The website is not safe.", exception.Message);
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithFetchServiceException_PropagatesException()
    {
        // Arrange
        var request = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "https://example.com/article",
            Category = "Science"
        };

        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(request.Url))
            .ThrowsAsync(new Exception("Network error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            async () => await _aiQuestionService.GenerateQuestionUsingWebURL(request)
        );

        Assert.Equal("Network error", exception.Message);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithValidationFailure_ReturnsErrorResponse()
    {
        // Arrange
        var request = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "https://example.com/inappropriate",
            Category = "Educational"
        };

        var webContent = "Inappropriate content from web";

        var validationResult = new ContentValidationResult
        {
            IsValid = false,
            IsMatch = false,
            Category = "inappropriate",
            Reason = "Content is not suitable",
            ValidationFailed = true
        };

        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(request.Url))
            .ReturnsAsync(webContent);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(webContent, request.Category))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _aiQuestionService.GenerateQuestionUsingWebURL(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Content validation failed: Content is not suitable", result.Message);
        Assert.Equal(400, result.StatusCode);
        Assert.NotNull(result.Validation);
        Assert.False(result.Validation.ShouldProceed);
        _mockGroqService.Verify(x => x.GenerateQuesions(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithNullCategory_UsesDefaultCategory()
    {
        // Arrange
        var request = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "https://example.com/article",
            Category = null,
            CategoryId = 1
        };

        var webContent = "Science article content";

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
            new QuizQuestionDto {
                QueText = "Test question?",
                QueTypeName = "Multiple Choice",
                QueDifficultyName = "Medium",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "A" },
                    new QueOption { Key = "answer", Value = "A" }
                }
             }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);

        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(request.Url))
            .ReturnsAsync(webContent);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(webContent, "educational"))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await _aiQuestionService.GenerateQuestionUsingWebURL(request);

        // Assert
        Assert.True(result.Success);
        _mockValidatorService.Verify(x => x.ValidateContentAsync(webContent, "educational"), Times.Once);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithNullQuestionSpec_UsesDefaultSpecs()
    {
        // Arrange
        var request = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "https://example.com/article",
            Category = "Science",
            QuestionSpec = null
        };

        var webContent = "Science article content";

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
            new QuizQuestionDto {
                QueText = "Test question?",
                QueTypeName = "Multiple Choice",
                QueDifficultyName = "Medium",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "A" },
                    new QueOption { Key = "answer", Value = "A" }
                }
             }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);

        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(request.Url))
            .ReturnsAsync(webContent);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(webContent, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await _aiQuestionService.GenerateQuestionUsingWebURL(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Specifications);
        Assert.Single(result.Specifications);
        Assert.Equal("Multiple Choice", result.Specifications[0].Type);
        Assert.Equal("Medium", result.Specifications[0].Difficulty);
        Assert.Equal(10, result.Specifications[0].Count);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithComplexQuestionSpec_GeneratesCorrectly()
    {
        // Arrange
        var request = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "https://example.com/article",
            Category = "Science",
            CategoryId = 3,
            QuestionSpec = new List<QuestionGenerationFormatDto>
            {
                new QuestionGenerationFormatDto
                {
                    QuestionDifficultyName = "Easy",
                    QuestionDifficultyId = 1,
                    QuestionPerQuestionType = new List<QuestionPerQuestionType>
                    {
                        new QuestionPerQuestionType
                        {
                            QuestionPerQuestionTypeName = "True/False",
                            QuestionPerQuestionTypeId = 2,
                            NoOfQuesitons = 3
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
                            QuestionPerQuestionTypeId = 4,
                            NoOfQuesitons = 2
                        }
                    }
                }
            }
        };

        var webContent = "Advanced physics concepts article";

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
            new QuizQuestionDto {
                QueText = "True or false question?",
                QueTypeName = "True/False",
                QueDifficultyName = "Easy",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "True" },
                    new QueOption { Key = "option", Value = "False" },
                    new QueOption { Key = "answer", Value = "True" }
                }
             },
            new QuizQuestionDto {
                QueText = "Fill in the blank question",
                QueTypeName = "Fill in the Blank",
                QueDifficultyName = "Hard",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "answer", Value = "answer" }
                }
             }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);

        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(request.Url))
            .ReturnsAsync(webContent);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(webContent, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await _aiQuestionService.GenerateQuestionUsingWebURL(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);
        Assert.NotNull(result.Specifications);
        Assert.Equal(2, result.Specifications.Count);

        // Verify ID mapping
        Assert.Equal(3, result.Data[0].CategoryId);
        Assert.Equal("Science", result.Data[0].CategoryName);
        Assert.Equal(1, result.Data[0].QueDifficultyId);
        Assert.Equal(2, result.Data[0].QueTypeId);

        Assert.Equal(3, result.Data[1].QueDifficultyId);
        Assert.Equal(4, result.Data[1].QueTypeId);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithLongContent_ProcessesSuccessfully()
    {
        // Arrange
        var longContent = new string('a', 10000); // Simulate long web content

        var request = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "https://example.com/long-article",
            Category = "Science"
        };

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
            new QuizQuestionDto {
                QueText = "Test question?",
                QueTypeName = "Multiple Choice",
                QueDifficultyName = "Medium",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "A" },
                    new QueOption { Key = "answer", Value = "A" }
                }
             }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);

        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(request.Url))
            .ReturnsAsync(longContent);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(longContent, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await _aiQuestionService.GenerateQuestionUsingWebURL(request);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithInvalidJsonFromGroq_ReturnsErrorResponse()
    {
        // Arrange
        var request = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "https://example.com/article",
            Category = "Science"
        };

        var webContent = "Science article content";

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false
        };

        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(request.Url))
            .ReturnsAsync(webContent);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(webContent, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync("Invalid JSON");

        // Act
        var result = await _aiQuestionService.GenerateQuestionUsingWebURL(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("AI response format error.", result.Message);
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_WithEmptyGroqResponse_ReturnsErrorResponse()
    {
        // Arrange
        var request = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "https://example.com/article",
            Category = "Science"
        };

        var webContent = "Science article content";

        var validationResult = new ContentValidationResult
        {
            IsValid = true,
            IsMatch = true,
            Category = "educational",
            Reason = "Valid content",
            ValidationFailed = false
        };

        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(request.Url))
            .ReturnsAsync(webContent);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(webContent, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync("[]");

        // Act
        var result = await _aiQuestionService.GenerateQuestionUsingWebURL(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed to generate questions.", result.Message);
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public async Task GenerateQuestionUsingWebURL_PassesCorrectParametersToGenerateFromPrompt()
    {
        // Arrange
        var request = new GenerateQuestionUsingWebRequestDTO
        {
            Url = "https://example.com/article",
            Category = "Mathematics",
            CategoryId = 7,
            QuestionSpec = new List<QuestionGenerationFormatDto>
            {
                new QuestionGenerationFormatDto
                {
                    QuestionDifficultyName = "Medium",
                    QuestionDifficultyId = 2,
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
            }
        };

        var webContent = "Mathematical concepts and formulas";

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
            new QuizQuestionDto {
                QueText = "Math question?",
                QueTypeName = "Multiple Choice",
                QueDifficultyName = "Medium",
                QueOptionsAns = new List<QueOption>
                {
                    new QueOption { Key = "option", Value = "A" },
                    new QueOption { Key = "answer", Value = "A" }
                }
             }
        };

        var jsonResponse = JsonSerializer.Serialize(generatedQuestions);

        _mockFetchContentFromUrlService
            .Setup(x => x.FetchAndValidateAsync(request.Url))
            .ReturnsAsync(webContent);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(webContent, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await _aiQuestionService.GenerateQuestionUsingWebURL(request);

        // Assert
        Assert.True(result.Success);

        // Verify that ValidateContentAsync was called with web content, not URL
        _mockValidatorService.Verify(
            x => x.ValidateContentAsync(webContent, "Mathematics"),
            Times.Once
        );

        // Verify the result contains correct mappings
        Assert.Equal(7, result.Data[0].CategoryId);
        Assert.Equal("Mathematics", result.Data[0].CategoryName);
        Assert.Equal(2, result.Data[0].QueDifficultyId);
        Assert.Equal(1, result.Data[0].QueTypeId);
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

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(extractedText, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

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

        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(extractedText);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(extractedText, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

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

        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(extractedText);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(extractedText, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

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

        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(extractedText);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(extractedText, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

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

        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(extractedText);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(extractedText, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

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

        _mockCommonService
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync(extractedText);

        _mockValidatorService
            .Setup(x => x.ValidateContentAsync(extractedText, request.Category))
            .ReturnsAsync(validationResult);

        _mockGroqService
            .Setup(x => x.GenerateQuesions(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

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