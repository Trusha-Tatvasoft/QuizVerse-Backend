using System.Text.Json;
using Microsoft.AspNetCore.Http;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Service;

public class AiQuestionGenerationService(IGroqService _groq, IGroqContentValidatorService _validator, IFetchContentFromUrlService _fetchContentFromUrlService) : IAiQuestionGenerationService
{
    public async Task<GenerateQuizResponseDto> GenerateFromPromptAsync(GenerateQuizRequest request)
    {
        try
        {
            var validation = await _validator.ValidateContentAsync(request.Prompt!, request.Category ?? "educational");
            if (!validation.ShouldProceed)
            {
                return new GenerateQuizResponseDto
                {
                    Success = false,
                    Message = string.Format(Constants.CONTENT_VALIDATION_FAILED, validation.Reason),
                    Validation = new ValidationInfoDto
                    {
                        ShouldProceed = validation.ShouldProceed,
                        Reason = validation.Reason,
                        Category = validation.Category
                    },
                    StatusCode = 400
                };
            }

            var specs = FlattenQuestionSpecs(request.QuestionSpec);
            if (specs.Count == 0)
                return new GenerateQuizResponseDto
                {
                    Success = false,
                    Message = Constants.INVALID_QUESTION_SPECIFICATION,
                    StatusCode = 400
                };

            var formattedPrompt = BuildPromptWithSpecs(request.Prompt!, specs);
            var rawJson = await _groq.GenerateQuesions(formattedPrompt);

            if (string.IsNullOrWhiteSpace(rawJson) || rawJson == "[]")
                return new GenerateQuizResponseDto
                {
                    Success = false,
                    Message = Constants.FAILED_TO_GENERATE_QUESTIONS,
                    StatusCode = 500
                };

            List<QuizQuestionDto>? quiz;
            try
            {
                quiz = JsonSerializer.Deserialize<List<QuizQuestionDto>>(rawJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
            catch (JsonException ex)
            {
                return new GenerateQuizResponseDto
                {
                    Success = false,
                    Message = Constants.AI_RESPONSE_FORMAT_ERROR,
                    Error = ex.Message,
                    RawResponse = rawJson,
                    StatusCode = 500
                };
            }

            // Map IDs to the generated questions
            quiz = MapIdsToGeneratedQuestions(quiz, request);

            return new GenerateQuizResponseDto
            {
                Success = true,
                Message = Constants.QUESTION_GENERATION_SUCCESS,
                Data = quiz,
                Count = quiz.Count,
                Specifications = specs,
                Validation = new ValidationInfoDto { Category = validation.Category },
                StatusCode = 200
            };
        }
        catch (Exception ex)
        {
            return new GenerateQuizResponseDto
            {
                Success = false,
                Message = Constants.UNEXPECTED_ERROR_GENERATING_QUESTIONS,
                Error = ex.Message,
                StatusCode = 500
            };
        }
    }
    
    public async Task<GenerateQuizResponseDto> GenerateQuestionUsingWebURL(GenerateQuestionUsingWebRequestDTO request)
    {
        if (string.IsNullOrEmpty(request.Url))
        {
            throw new AppException(Constants.INVALID_URL_PROVIDED, StatusCodes.Status404NotFound);
        }

        string Content = await _fetchContentFromUrlService.FetchAndValidateAsync(request.Url!);

        if (string.IsNullOrEmpty(Content))
        {
            throw new AppException(Constants.WEB_CONTENT_NOT_FOUND, StatusCodes.Status404NotFound);
        }
        
        GenerateQuizRequest questionGenerationRequest = new GenerateQuizRequest
        {
            Prompt = Content,
            QuestionSpec = request.QuestionSpec,
            Category = request.Category,
            CategoryId = request.CategoryId
        };
        GenerateQuizResponseDto response = await GenerateFromPromptAsync(questionGenerationRequest);

        return response;
    }

    private List<QuizQuestionDto> MapIdsToGeneratedQuestions(List<QuizQuestionDto> quiz, GenerateQuizRequest request)
    {
        // Set category information for all questions
        foreach (var question in quiz)
        {
            question.CategoryId = request.CategoryId;
            question.CategoryName = request.Category;

            // Find matching difficulty and question type
            if (request.QuestionSpec != null)
            {
                foreach (var difficultyGroup in request.QuestionSpec)
                {
                    // Check if difficulty name matches (case insensitive)
                    if (string.Equals(difficultyGroup.QuestionDifficultyName, question.QueDifficultyName, StringComparison.OrdinalIgnoreCase))
                    {
                        question.QueDifficultyId = difficultyGroup.QuestionDifficultyId;
                        question.QueDifficultyName = difficultyGroup.QuestionDifficultyName;

                        // Find matching question type
                        if (difficultyGroup.QuestionPerQuestionType != null)
                        {
                            foreach (var questionType in difficultyGroup.QuestionPerQuestionType)
                            {
                                if (string.Equals(questionType.QuestionPerQuestionTypeName, question.QueTypeName, StringComparison.OrdinalIgnoreCase))
                                {
                                    question.QueTypeId = questionType.QuestionPerQuestionTypeId;
                                    question.QueTypeName = questionType.QuestionPerQuestionTypeName;
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }

        return quiz;
    }

    private List<QuestionSpecification> FlattenQuestionSpecs(List<QuestionGenerationFormatDto>? specs)
    {
        if (specs == null || specs.Count == 0)
        {
            return
            [
                new QuestionSpecification
                {
                    Type = "Multiple Choice",
                    Difficulty = "Medium",
                    Count = 10
                }
            ];
        }

        var result = new List<QuestionSpecification>();

        foreach (var group in specs)
        {
            if (group.QuestionPerQuestionType == null) continue;

            foreach (var qtype in group.QuestionPerQuestionType)
            {
                if (qtype.NoOfQuesitons > 0)
                {
                    result.Add(new QuestionSpecification
                    {
                        Type = qtype.QuestionPerQuestionTypeName,
                        Difficulty = group.QuestionDifficultyName,
                        Count = qtype.NoOfQuesitons
                    });
                }
            }
        }

        return result;
    }

    private string BuildPromptWithSpecs(string inputText, List<QuestionSpecification> specs)
    {
        var totalQuestions = specs.Sum(s => s.Count);
        var specificationCount = 1;
        var specsList = specs.Select(s =>
            $"{specificationCount++}. {s.Count} questions of {s.Type} type with {s.Difficulty.ToUpper()} difficulty.");

        string specificationsText = string.Join("\n\t", specsList);

        string difficultyGuidelines = PromptConstants.DIFFICULTY_GUIDLINES;

        return string.Format(
            PromptConstants.QUIZ_QUESTION_GENERATION_PROMPT,
            totalQuestions,
            specificationsText,
            difficultyGuidelines,
            inputText
        );
    }
}