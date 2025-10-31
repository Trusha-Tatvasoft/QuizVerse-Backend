using System.Text.Json;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Service;

public class AiQuestionGenerationService(IGroqService _groq, IGroqContentValidatorService _validator) : IAiQuestionGenerationService
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

    // ✅ Converts your nested question spec to a flat format
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

    // ✅ Updated prompt builder to enforce your required JSON format
    private string BuildPromptWithSpecs(string inputText, List<QuestionSpecification> specs)
    {
        var totalQuestions = specs.Sum(s => s.Count);

        var specsList = specs.Select(s =>
            $"- {s.Count} {s.Type} questions at {s.Difficulty.ToUpper()} difficulty level");

        string specificationsText = string.Join("\n", specsList);

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