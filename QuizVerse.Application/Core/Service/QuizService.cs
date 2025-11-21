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
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
namespace QuizVerse.Application.Core.Service;


public class QuizService(
    IGenericRepository<QuizPlayStatus> _quizPlayStatusRepository,
    IGenericRepository<AttemptedQuizQuestionsAnswer> _attemptedQuizQuestionsAnswerRepository,
    IGenericRepository<Quiz> _quizRepostory,
    IGenericRepository<QuizToBaseQuestionMap> _quizToBaseQuestionMapRepository,
    IGenericRepository<QuizAttempted> _quizAttemptedRepository,
    IGenericRepository<QuestionIssueReport> _questionIssueReportRepository,
    IGenericRepository<QuizIssueReport> _quizIssueReportRepository,
    IGenericRepository<QuizRating> _quizRatingRepo,
    IMapper _mapper,
    ISqlQueryRepository _sqlQueryRepository,
    IHttpContextAccessor _httpContextAccessor,
    IAiService _aiService,
    ILeaderboardService _leaderboardService,
    IGcpApiQueueService _queueService
) : IQuizService
{
    public int UserId => _httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);
    public async Task<QuizOverviewResponseDto> GetQuizOverviewAsync(int quizId)
    {
        // Fetch the quiz including related Category and DifficultyLevel
        Quiz quiz = await _quizRepostory
            .GetQueryableInclude(q => q.Category, q => q.DifficultyLevel)  // include navigation properties
            .OrderBy(q => q.Id)
            .FirstOrDefaultAsync(q => q.Id == quizId && q.Status == (int)QuizStatus.Active) ?? throw new AppException(Constants.QUIZ_NOT_FOUND);  // fetch single quiz by ID

        // Map entity to DTO
        QuizOverviewResponseDto quizDto = _mapper.Map<QuizOverviewResponseDto>(quiz);

        return quizDto;
    }

    public async Task<QuizStartResponseDto?> StartQuizAsync(int quizId)
    {
        bool exists = await _quizRepostory.Exists(q => q.Id == quizId && q.Status == (int)QuizStatus.Active);
        if (!exists) throw new AppException(Constants.QUIZ_NOT_FOUND);
        RawStartQuizDto raw = await _sqlQueryRepository.SqlQuerySingleAsync<RawStartQuizDto>(string.Format(SqlConstants.START_QUIZ_QUERY_TEMPLATE, quizId, UserId));
        if (raw == null) return null;

        List<OptionResponseDto> options = JsonSerializer.Deserialize<List<OptionResponseDto>>(raw.Options ?? "[]") ?? [];
        QuizStartResponseDto quizQuestionDto = _mapper.Map<QuizStartResponseDto>(raw);
        quizQuestionDto.Options = options;

        return quizQuestionDto;
    }

    public async Task<QuizQuestionResponseDto?> SaveAndNextQuestion(SaveAndNextQuestionRequestDto request)
    {
        // Get current quiz play status
        QuizPlayStatus quizPlayStatus = await _quizPlayStatusRepository
                            .GetAsync(q => q.QuizId == request.QuizId
                              && q.UserId == UserId /* current user id */
                              && q.IsCompleted == false)
                            ?? throw new AppException(Constants.QUIZ_NOT_FOUND_OR_COMPLETED);
        Quiz quiz = await _quizRepostory.GetAsync(q => q.Id == request.QuizId)
            ?? throw new AppException(Constants.QUIZ_NOT_FOUND);
        // Get current question
        QuizToBaseQuestionMap currentQuestionMap = await _quizToBaseQuestionMapRepository
            .GetQueryableInclude(q => q.Que)
            .Include("Que.QuestionOptionsAnswers")
            .FirstOrDefaultAsync(q => q.Id == request.CurrentQuestionId && q.QuizId == request.QuizId)
            ?? throw new AppException(Constants.QUESTION_NOT_FOUND);

        BaseQuestion question = currentQuestionMap.Que;

        bool isCorrect = await ValidateAnswer(question, request.GivenAnswer);

        await SaveOrUpdateAttempt(quizPlayStatus, currentQuestionMap, question, request.GivenAnswer, isCorrect);
        if (request.NextQuestionNumber <= quiz.TotalQuestion)
        {
            // Fetch next question
            RawQuizQuestionDto raw = await _sqlQueryRepository.SqlQuerySingleAsync<RawQuizQuestionDto>(string.Format(SqlConstants.GET_QUIZ_QUESTIONS_QUERY_TEMPLATE, request.QuizId, request.NextQuestionNumber));

            List<OptionResponseDto> options = JsonSerializer.Deserialize<List<OptionResponseDto>>(raw.Options ?? "[]") ?? [];

            QuizQuestionResponseDto quizResponseDto = _mapper.Map<QuizQuestionResponseDto>(raw);
            quizResponseDto.Options = options;
            return quizResponseDto;
        }
        return null;
    }

    private async Task<bool> CheckAnswer(QuizAnswerCheckDto quizAnswerCheck)
    {
        if (string.IsNullOrWhiteSpace(quizAnswerCheck.GivenAnswer))
            return false;

        string prompt = string.Format(
            PromptConstants.CHECK_ANSWER_PROMPT,
            quizAnswerCheck.QuestionName,
            quizAnswerCheck.CorrectAnswer,
            quizAnswerCheck.GivenAnswer
        );

        string response = await _aiService.GetResponseAsync(prompt);

        return response.Trim().StartsWith("TRUE", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> SubmitQuiz(SubmitQuizRequestDTO request)
    {
        QuizPlayStatus quizPlayStatus = await _quizPlayStatusRepository
                            .GetAsync(q => q.QuizId == request.QuizId
                              && q.UserId == UserId
                              && q.IsCompleted == false)
                            ?? throw new AppException(Constants.QUIZ_NOT_FOUND_OR_COMPLETED);

        QuizToBaseQuestionMap currentQuestionMap = await _quizToBaseQuestionMapRepository
            .GetQueryableInclude(q => q.Que)
            .Include("Que.QuestionOptionsAnswers")
            .FirstOrDefaultAsync(q => q.Id == request.LastVisitedQuestionAndAnswers.QuestionId && q.QuizId == request.QuizId)
            ?? throw new AppException(Constants.QUESTION_NOT_FOUND);

        BaseQuestion question = currentQuestionMap.Que;
        bool isCorrect = await ValidateAnswer(question, request.LastVisitedQuestionAndAnswers.GivenAnswer);

        await SaveOrUpdateAttempt(quizPlayStatus, currentQuestionMap, question, request.LastVisitedQuestionAndAnswers.GivenAnswer, isCorrect);

        quizPlayStatus.IsCompleted = true;
        quizPlayStatus.ModifiedDate = DateTime.UtcNow;
        await _quizPlayStatusRepository.UpdateAsync(quizPlayStatus);

        await _sqlQueryRepository.SqlQuerySingleAsync<SuccessResponseDTO>(string.Format(SqlConstants.QUIZ_ATTEMPT_COMPLETE_FUNCTION, request.QuizId, UserId, request.TimeTaken));
        await _sqlQueryRepository.SqlQuerySingleAsync<SuccessResponseDTO>(string.Format(SqlConstants.RECALC_USER_STREAK_FUNCTION, UserId));
        await _sqlQueryRepository.SqlQuerySingleAsync<SuccessResponseDTO>(SqlConstants.RECALC_GLOBAL_RANKS_FUNCTION);
        await _sqlQueryRepository.SqlQuerySingleAsync<SuccessResponseDTO>(string.Format(SqlConstants.CHECK_AND_AWARD_BADGES_FUNCTION, UserId));

        _leaderboardService.ClearAvailableYearsCache();
        _leaderboardService.ClearAvailableMonthsCache(DateTime.UtcNow.Year);

        return true;
    }

    private async Task<bool> ValidateAnswer(BaseQuestion question, string? givenAnswer)
    {
        if (question.QueTypeId == 3 || question.QueTypeId == 4)
        {
            QuizAnswerCheckDto quizAnswerCheck = new()
            {
                QuestionName = question.QueText,
                GivenAnswer = givenAnswer ?? "",
                CorrectAnswer = string.Join(", ", question.QuestionOptionsAnswers
                                            .Select(o => o.Value))
            };

            return await CheckAnswer(quizAnswerCheck);
        }

        // Objective/MCQ
        string correctAnswer = question.QuestionOptionsAnswers
                                .Where(o => o.Key.Equals("answer", StringComparison.OrdinalIgnoreCase))
                                .Select(o => o.Value)
                                .FirstOrDefault() ?? "";

        return string.Equals(givenAnswer?.Trim(), correctAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private async Task SaveOrUpdateAttempt(
        QuizPlayStatus quizPlayStatus,
        QuizToBaseQuestionMap currentQuestionMap,
        BaseQuestion question,
        string? givenAnswer,
        bool isCorrect
    )
    {
        AttemptedQuizQuestionsAnswer? attemptedAnswer = await _attemptedQuizQuestionsAnswerRepository
            .GetAsync(a => a.QuizPlayStatusId == quizPlayStatus.Id
                        && a.QuizQueId == currentQuestionMap.Id);

        if (attemptedAnswer != null)
        {
            attemptedAnswer.GivenAnswer = givenAnswer;
            attemptedAnswer.IsCorrect = isCorrect;
            attemptedAnswer.ModifiedDate = DateTime.UtcNow;
            await _attemptedQuizQuestionsAnswerRepository.UpdateAsync(attemptedAnswer);
        }
        else
        {
            attemptedAnswer = new AttemptedQuizQuestionsAnswer()
            {
                QuizPlayStatusId = quizPlayStatus.Id,
                QuizQueId = currentQuestionMap.Id,
                QueTypeId = question.QueTypeId,
                GivenAnswer = givenAnswer,
                IsCorrect = isCorrect,
                QuestionText = question.QueText,
                QuestionAnswer = question.QuestionOptionsAnswers
                                    .Where(o => o.Key.Equals("answer"))
                                    .Select(o => o.Value)
                                    .FirstOrDefault() ?? "",
                CreatedDate = DateTime.UtcNow
            };
            await _attemptedQuizQuestionsAnswerRepository.AddAsync(attemptedAnswer);
        }
    }

    public async Task<QuizCompletedSummaryDTO> GetQuizSummary(int quizId)
    {
        QuizAttempted attempt = await _quizAttemptedRepository
            .GetQueryableInclude(a => a.Quiz, a => a.GradeNavigation)
            .FirstOrDefaultAsync(a => a.QuizId == quizId && a.UserId == UserId)
            ?? throw new AppException(Constants.QUIZ_ATTEMPT_NOT_FOUND);

        QuizCompletedSummaryDTO summaryDto = _mapper.Map<QuizCompletedSummaryDTO>(attempt);

        return summaryDto;
    }

    #region Get Question Attempted
    public async Task<List<QuizQuestionReviewDTO>> GetQuizQuestionReview(int quizId)
    {
        List<QuizQuestionReviewDTO> quizQuestionReviews = await _sqlQueryRepository.SqlQueryListAsync<QuizQuestionReviewDTO>(string.Format(
            SqlConstants.GET_QUIZ_QUESTION_REVIEW_FUNCTION,
            quizId,
            UserId));

        quizQuestionReviews.ForEach(q =>
        {
            if (string.IsNullOrWhiteSpace(q.UserAnswer))
            {
                q.UserAnswer = null;
                q.IsCorrect = null;
            }
        });

        return quizQuestionReviews;
    }
    #endregion

    #region Question issue report
    public async Task<string> CreateOrUpdateQuestionIssueReport(QuestionIssueReportRequestDTO request)
    {
        // Validate quiz existence
        bool quizExists = await _quizRepostory.Exists(q =>
            q.Id == request.QuizId &&
            q.Status == (int)QuizStatus.Active &&
            !q.IsDeleted);

        if (!quizExists)
        {
            throw new AppException(Constants.QUIZ_NOT_FOUND);
        }

        // ----- CREATE -----
        if (request.ReportId == null || request.ReportId == 0)
        {

            QuestionIssueReport questionIssueReport = new()
            {
                UserId = UserId,
                QuizId = request.QuizId,
                QuestionId = request.QuestionId,
                Description = request.Description,
                Severity = (int)QuestionOrQuizIssueReportSeverity.UnderProcessing,
                Status = (int)QuestionOrQuizIssueReportStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            await _questionIssueReportRepository.AddAsync(questionIssueReport);

            if (questionIssueReport.Id <= 0)
            {
                throw new AppException(Constants.QUESTION_ISSUE_REPORT_SUBMISSION_FAILED, 400);
            }

            // Push to queue
            GcpApiReportDataDto questionReport = new()
            {
                ReportId = questionIssueReport.Id,
                ReportType = ReportType.QuestionIssueReport,
                ReportComment = request.Description,
            };

            await _queueService.EnqueueAsync(questionReport);

            return Constants.QUESTION_ISSUE_REPORTED;
        }

        // ----- UPDATE -----
        QuestionIssueReport existingReport = await _questionIssueReportRepository.GetAsync(q =>
            q.Id == request.ReportId &&
            q.UserId == UserId &&
            q.Severity != (int)QuestionOrQuizIssueReportSeverity.UnderProcessing &&
            q.Status == (int)QuestionOrQuizIssueReportStatus.Pending)
            ?? throw new AppException(Constants.QUESTION_ISSUE_REPORT_NOT_FOUND_OR_CANNOT_EDIT);

        existingReport.Description = request.Description;
        existingReport.Severity = (int)QuestionOrQuizIssueReportSeverity.UnderProcessing;
        existingReport.Status = (int)QuestionOrQuizIssueReportStatus.Pending;
        existingReport.CreatedDate = DateTime.UtcNow;

        await _questionIssueReportRepository.UpdateAsync(existingReport);

        if (existingReport.Id <= 0)
        {
            throw new AppException(Constants.QUESTION_ISSUE_REPORT_SUBMISSION_FAILED, 400);
        }

        // Push edit to queue
        GcpApiReportDataDto questionEditReport = new()
        {
            ReportId = existingReport.Id,
            ReportType = ReportType.QuestionIssueReport,
            ReportComment = request.Description,
        };

        await _queueService.EnqueueAsync(questionEditReport);

        return Constants.QUESTION_ISSUE_UPDATED;
    }
    #endregion

    #region Get Question Report
    public async Task<QuestionIssueReportResponseDTO> GetQuizReportQuestionIssue(int reportId)
    {
        QuestionIssueReport report = await _questionIssueReportRepository.GetAsync(q => q.Id == reportId)
        ?? throw new AppException(Constants.QUESTION_ISSUE_REPORT_NOT_FOUND);

        return _mapper.Map<QuestionIssueReportResponseDTO>(report);
    }
    #endregion

    public async Task<QuizRatingDTO?> GetMyQuizRating(int quizId)
    {
        QuizRating? rating = await _quizRatingRepo.GetAsync(r => r.QuizId == quizId && r.UserId == UserId);

        return rating == null ? null : _mapper.Map<QuizRatingDTO>(rating);
    }

    public async Task<string> SubmitQuizRating(QuizRatingDTO request)
    {
        bool alreadyExists = await _quizRatingRepo.Exists(r => r.UserId == UserId && r.QuizId == request.QuizId);

        if (alreadyExists)
            throw new AppException(Constants.DUPLICATE_QUIZ_RATING);

        QuizRating entity = _mapper.Map<QuizRating>(request);
        entity.UserId = UserId;

        await _quizRatingRepo.AddAsync(entity);

        return Constants.QUIZ_RATING_SUBMITTED;
    }

    public async Task<string> GetAnswerExplanation(AnswerExplanationRequestDTO request)
    {
        string answerText = string.IsNullOrWhiteSpace(request.UserAnswer)
            ? Constants.NO_ANSWER_PROVIDED
            : request.UserAnswer;

        string prompt = string.Format(
            PromptConstants.GET_EXPLANATION_PROMPT,
            request.QuestionText,
            request.CorrectAnswer,
            answerText
        );

        string response = await _aiService.GetResponseAsync(prompt);

        return response.Trim();
    }

    public async Task<bool> AddEditQuizReport(QuizReportRequestDto quizReportRequestDto)
    {
        bool quizExists = await _quizRepostory.Exists(q => q.Id == quizReportRequestDto.QuizId && q.Status == (int)QuizStatus.Active && q.IsDeleted == false);
        if (!quizExists)
        {
            throw new AppException(Constants.QUIZ_NOT_FOUND);
        }
        if (quizReportRequestDto.ReportId == 0 || quizReportRequestDto.ReportId == null)
        {
            QuizIssueReport quizIssueReport = new QuizIssueReport
            {
                UserId = UserId,
                QuizId = quizReportRequestDto.QuizId,
                Reason = quizReportRequestDto.Reason,
                Severity = (int)QuestionOrQuizIssueReportSeverity.UnderProcessing,
                Status = (int)QuestionOrQuizIssueReportStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            await _quizIssueReportRepository.AddAsync(quizIssueReport);

            if (quizIssueReport.Id > 0 == false)
            {
                throw new AppException(Constants.QUIZ_REPORT_SUBMISSION_FAILED, 400);
            }

            GcpApiReportDataDto QuizReports = new()
            {
                ReportId = quizIssueReport.Id,
                ReportType = ReportType.QuizIssueReport,
                ReportComment = quizReportRequestDto.Reason,
            };

            await _queueService.EnqueueAsync(QuizReports);

            return true;
        }
        QuizIssueReport existingReport = await _quizIssueReportRepository.GetAsync(q => q.Id == quizReportRequestDto.ReportId
                                && q.UserId == UserId
                                && q.Severity != (int)QuestionOrQuizIssueReportSeverity.UnderProcessing
                                && q.Status == (int)QuestionOrQuizIssueReportStatus.Pending)
            ?? throw new AppException(Constants.QUIZ_REPORT_NOT_FOUND_OR_CANNOT_EDIT);

        existingReport.Reason = quizReportRequestDto.Reason;
        existingReport.Severity = (int)QuestionOrQuizIssueReportSeverity.UnderProcessing;
        existingReport.Status = (int)QuestionOrQuizIssueReportStatus.Pending;
        existingReport.CreatedDate = DateTime.UtcNow;
        await _quizIssueReportRepository.UpdateAsync(existingReport);
        if (existingReport.Id > 0 == false)
        {
            throw new AppException(Constants.QUIZ_REPORT_SUBMISSION_FAILED, 400);
        }

        GcpApiReportDataDto QuizEditReports = new()
        {
            ReportId = existingReport.Id,
            ReportType = ReportType.QuizIssueReport,
            ReportComment = quizReportRequestDto.Reason,
        };

        await _queueService.EnqueueAsync(QuizEditReports);
        return true;
    }
}