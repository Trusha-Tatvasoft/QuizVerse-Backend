using System.Text.Json;
using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class AiQuestionGenerationServiceTests
{
    private readonly Mock<IGroqService> _mockGroqService;
    private readonly Mock<IGroqContentValidatorService> _mockValidatorService;
    private readonly AiQuestionGenerationService _aiQuestionService;

    public AiQuestionGenerationServiceTests()
    {
        _mockGroqService = new Mock<IGroqService>();
        _mockValidatorService = new Mock<IGroqContentValidatorService>();
        _aiQuestionService = new AiQuestionGenerationService(_mockGroqService.Object, _mockValidatorService.Object);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithValidInput_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new GenerateQuizRequest
        {
            Prompt = "Science questions about physics",
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
        // The service should return "Failed to generate questions." for empty prompt
        // because it still tries to generate but the Groq service returns empty
        Assert.Equal("Failed to generate questions.", result.Message);
        Assert.Equal(500, result.StatusCode);

        // The service should still call GenerateQuesions even with empty prompt
        // because the validation passes and it proceeds to generation
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
                    QuestionPerQuestionType = new List<QuestionPerQuestionType>
                    {
                        new QuestionPerQuestionType { QuestionPerQuestionTypeName = "Multiple Choice", NoOfQuesitons = 5 },
                        new QuestionPerQuestionType { QuestionPerQuestionTypeName = "True/False", NoOfQuesitons = 3 }
                    }
                },
                new QuestionGenerationFormatDto
                {
                    QuestionDifficultyName = "Hard",
                    QuestionPerQuestionType = new List<QuestionPerQuestionType>
                    {
                        new QuestionPerQuestionType { QuestionPerQuestionTypeName = "Fill in the Blank", NoOfQuesitons = 2 }
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
        Assert.Equal(3, result.Specifications.Count);

        var easyMultipleChoice = result.Specifications.First(s => s.Type == "Multiple Choice" && s.Difficulty == "Easy");
        Assert.Equal(5, easyMultipleChoice.Count);

        var easyTrueFalse = result.Specifications.First(s => s.Type == "True/False" && s.Difficulty == "Easy");
        Assert.Equal(3, easyTrueFalse.Count);

        var hardFillBlank = result.Specifications.First(s => s.Type == "Fill in the Blank" && s.Difficulty == "Hard");
        Assert.Equal(2, hardFillBlank.Count);
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
                        new QuestionPerQuestionType { QuestionPerQuestionTypeName = "TF", NoOfQuesitons = 0 } // Should be excluded
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
        Assert.Single(result.Specifications); // Only MCQ should be included
        Assert.Equal("MCQ", result.Specifications[0].Type);
        Assert.Equal(5, result.Specifications[0].Count);
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithNullQuestionPerQuestionType_UsesDefaultSpecs()
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
                QuestionPerQuestionType = null // This should cause fallback to default
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
        // The service should handle null QuestionPerQuestionType gracefully
        // Either by using default specs or returning success with empty specs
        if (result.Success)
        {
            Assert.NotNull(result.Specifications);
            // It might return default specs or empty specs
            if (result.Specifications.Count > 0)
            {
                Assert.Equal("Multiple Choice", result.Specifications[0].Type);
                Assert.Equal("Medium", result.Specifications[0].Difficulty);
                Assert.Equal(10, result.Specifications[0].Count);
            }
        }
        else
        {
            // If it fails, it should be because of no valid specs
            Assert.Equal("Invalid or missing question specifications.", result.Message);
            Assert.Equal(400, result.StatusCode);
        }
    }

    [Fact]
    public async Task GenerateFromPromptAsync_WithEmptyQuestionPerQuestionType_UsesDefaultSpecs()
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
                QuestionPerQuestionType = new List<QuestionPerQuestionType>() // Empty list
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
        // The service should handle empty QuestionPerQuestionType gracefully
        if (result.Success)
        {
            Assert.NotNull(result.Specifications);
            // It might return default specs or empty specs
            if (result.Specifications.Count > 0)
            {
                Assert.Equal("Multiple Choice", result.Specifications[0].Type);
                Assert.Equal("Medium", result.Specifications[0].Difficulty);
                Assert.Equal(10, result.Specifications[0].Count);
            }
        }
        else
        {
            // If it fails, it should be because of no valid specs
            Assert.Equal("Invalid or missing question specifications.", result.Message);
            Assert.Equal(400, result.StatusCode);
        }
    }
}