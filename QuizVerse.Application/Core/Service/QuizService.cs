using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
namespace QuizVerse.Application.Core.Service;

public class QuizService(IGenericRepository<QuizPlayStatus> _quizPlayStatusRepository, IGenericRepository<AttemptedQuizQuestionsAnswer> _attemptedQuizQuestionsAnswerRepository, IGenericRepository<Quiz> _quizRepostory, IGenericRepository<QuizToBaseQuestionMap> _quizToBaseQuestionMapRepository, IMapper _mapper, ISqlQueryRepository _sqlQueryRepository, IHttpContextAccessor _httpContextAccessor, IAiService _aiService) : IQuizService
{
    public int UserId => _httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);
    public async Task<QuizOverviewResponseDto> GetQuizOverviewAsync(int quizId)
    {
        // Fetch the quiz including related Category and DifficultyLevel
        Quiz quiz = await _quizRepostory
            .GetQueryableInclude(q => q.Category, q => q.DifficultyLevel)  // include navigation properties
            .OrderBy(q => q.Id)
            .FirstOrDefaultAsync(q => q.Id == quizId) ?? throw new AppException(Constants.QUIZ_NOT_FOUND);  // fetch single quiz by ID

        // Map entity to DTO
        QuizOverviewResponseDto quizDto = _mapper.Map<QuizOverviewResponseDto>(quiz);

        return quizDto;
    }

    public async Task<QuizStartResponseDto?> StartQuizAsync(int quizId)
    {
        string sql = $"SELECT * FROM start_quiz({quizId}, {UserId})";

        RawStartQuizDto raw = await _sqlQueryRepository.SqlQuerySingleAsync<RawStartQuizDto>(sql);
        if (raw == null) return null;

        List<OptionResponseDto> options = JsonSerializer.Deserialize<List<OptionResponseDto>>(raw.Options ?? "[]") ?? [];
        QuizStartResponseDto quizQuestionDto = _mapper.Map<QuizStartResponseDto>(raw);
        quizQuestionDto.Options = options;

        return quizQuestionDto;
    }

    public async Task<QuizQuestionResponseDto> SaveAndNextQuestion(SaveAndNextQuestionRequestDto request)
    {
        // Get current quiz play status
        QuizPlayStatus quizPlayStatus = await _quizPlayStatusRepository
                            .GetAsync(q => q.QuizId == request.QuizId
                              && q.UserId == UserId /* current user id */
                              && q.IsCompleted == false)
                            ?? throw new AppException(Constants.QUIZ_NOT_FOUND_OR_COMPLETED);

        // Get current question
        QuizToBaseQuestionMap currentQuestionMap = await _quizToBaseQuestionMapRepository
            .GetQueryableInclude(q => q.Que)
            .Include("Que.QuestionOptionsAnswers")
            .FirstOrDefaultAsync(q => q.Id == request.CurrentQuestionId && q.QuizId == request.QuizId)
            ?? throw new AppException(Constants.QUESTION_NOT_FOUND);

        BaseQuestion question = currentQuestionMap.Que;

        bool isCorrect = false;

        // Determine if answer is correct
        if (question.QueTypeId == 3 || question.QueTypeId == 4) // Subjective
        {
            QuizAnswerCheckDto quizAnswerCheck = new()
            {
                QuestionName = question.QueText,
                GivenAnswer = request.GivenAnswer,
                CorrectAnswer = string.Join(", ", question.QuestionOptionsAnswers
                                            .Where(o => !o.IsDeleted)
                                            .Select(o => o.Value))
            };

            isCorrect = await CheckAnswer(quizAnswerCheck);
        }
        else // MCQ / Objective
        {
            string correctAnswer = question.QuestionOptionsAnswers
                                    .Where(o => !o.IsDeleted && o.Key.Equals("answer", StringComparison.OrdinalIgnoreCase))
                                    .Select(o => o.Value)
                                    .FirstOrDefault() ?? "";

            isCorrect = string.Equals(request.GivenAnswer?.Trim(), correctAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        // Check if question was already attempted
        AttemptedQuizQuestionsAnswer? attemptedAnswer = await _attemptedQuizQuestionsAnswerRepository
            .GetAsync(a => a.QuizPlayStatusId == quizPlayStatus.Id && a.QuizQueId == currentQuestionMap.Id);

        if (attemptedAnswer != null)
        {
            // Update existing answer
            attemptedAnswer.GivenAnswer = request.GivenAnswer;
            attemptedAnswer.IsCorrect = isCorrect;
            attemptedAnswer.ModifiedDate = DateTime.UtcNow;
            await _attemptedQuizQuestionsAnswerRepository.UpdateAsync(attemptedAnswer);
        }
        else
        {
            // Insert new answer
            attemptedAnswer = new AttemptedQuizQuestionsAnswer()
            {
                QuizPlayStatusId = quizPlayStatus.Id,
                QuizQueId = currentQuestionMap.Id,
                QueTypeId = question.QueTypeId,
                GivenAnswer = request.GivenAnswer,
                IsCorrect = isCorrect,
                CreatedDate = DateTime.UtcNow
            };
            await _attemptedQuizQuestionsAnswerRepository.AddAsync(attemptedAnswer);
        }

        // Fetch next question

        string sql = $"SELECT * FROM get_quiz_questions({request.QuizId}, {request.NextQuestionNumber})";

        RawQuizQuestionDto raw = await _sqlQueryRepository.SqlQuerySingleAsync<RawQuizQuestionDto>(sql);

        List<OptionResponseDto> options = JsonSerializer.Deserialize<List<OptionResponseDto>>(raw.Options ?? "[]") ?? [];

        QuizQuestionResponseDto quizResponseDto = _mapper.Map<QuizQuestionResponseDto>(raw);
        quizResponseDto.Options = options;

        return quizResponseDto;
    }

    private async Task<bool> CheckAnswer(QuizAnswerCheckDto quizAnswerCheck)
    {
        if (string.IsNullOrWhiteSpace(quizAnswerCheck.GivenAnswer))
            return false;

        string prompt = $@"
        Evaluate the answer for a quiz question.

        Question: {quizAnswerCheck.QuestionName}

        Expected Answer: {quizAnswerCheck.CorrectAnswer}
        Provided Answer: {quizAnswerCheck.GivenAnswer}

        Instructions:
        - Consider the context of the question.
        - If the provided answer correctly answers the question and conveys the same meaning as the expected answer (even with different wording), return TRUE.
        - If the provided answer is incorrect, incomplete, or unrelated to the question, return FALSE.
        - Respond only with TRUE or FALSE.";

        string response = await _aiService.GetResponseAsync(prompt);

        return response.Trim().StartsWith("TRUE", StringComparison.OrdinalIgnoreCase);
    }
}