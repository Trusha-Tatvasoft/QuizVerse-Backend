using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class UserDashboardService(
    IGenericRepository<BattleRequest> _battleRequestRepository,
    IGenericRepository<Quiz> _quizRepository,
    IGenericRepository<QuizAttempted> _quizAttemptedRepository,
    ISqlQueryRepository _sqlQueryRepository,
    IHttpContextAccessor httpContextAccessor,
    IMapper _mapper
) : IUserDashboardService
{
    private int UserId => httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

    public async Task<UserDashboardResponse> GetStatisticsData()
    {
        RawUserDashboardMetricsDTO raw = await _sqlQueryRepository.SqlQuerySingleAsync<RawUserDashboardMetricsDTO>(string.Format(SqlConstants.GET_USER_DASHBOARD_METRICS, UserId));

        return _mapper.Map<UserDashboardResponse>(raw);
    }

    public async Task<List<RecentQuizResponse>> GetRecentQuizzes(bool viewAll)
    {
        IQueryable<QuizAttempted> query = _quizAttemptedRepository
            .GetQueryableInclude(qa => qa.Quiz.Category, qa => qa.Quiz.DifficultyLevel)
            .Where(qa => qa.UserId == UserId)
            .OrderByDescending(qa => qa.CreatedDate);

        if (!viewAll)
        {
            query = query.Take(3);
        }

        List<RecentQuizResponse> results = await query.Select(qa => new RecentQuizResponse
        {
            QuizId = qa.QuizId,
            QuizName = qa.Quiz.Name,
            CategoryName = qa.Quiz.Category.CategoryName,
            DifficultyLevel = qa.Quiz.DifficultyLevel.Name,
            Score = qa.TotalQue == 0
                ? 0
                : Math.Round(qa.CorrectedQue * 100.0 / qa.TotalQue, 2),
            AttemptedOn = qa.CreatedDate
        }).ToListAsync();

        return results;
    }

    public async Task<FeaturedQuizListDTO> GetFeaturedQuizzes(int batchNumber = 1)
    {
        if (batchNumber < Constants.MIN_BATCH || batchNumber > Constants.MAX_BATCH)
            throw new AppException(string.Format(Constants.INVALID_BATCH_NUMBER, Constants.MIN_BATCH, Constants.MAX_BATCH), StatusCodes.Status404NotFound);

        int skip = (batchNumber - 1) * Constants.BATCH_SIZE;

        DateTime today = DateTime.UtcNow.Date;
        DateTime tomorrow = today.AddDays(1);

        IQueryable<Quiz> query = _quizRepository
            .GetQueryableInclude(q => q.Category, q => q.DifficultyLevel, q => q.QuizAttempteds)
            .Where(q => q.IsFeatured
                        && q.Status == (int)QuizStatus.Active
                        && !q.IsDeleted
                        && q.CreatedDate >= today
                        && q.CreatedDate < tomorrow)
            .OrderByDescending(q => q.QuizAttempteds.Count())
            .ThenByDescending(q => q.Rating);

        int totalAvailable = await query.CountAsync();
        int totalToServe = Math.Min(totalAvailable, Constants.MAX_QUIZZES);

        if (skip >= totalToServe)
        {
            return new FeaturedQuizListDTO { Quizzes = [], HasMore = false };
        }

        List<FeaturedQuizDTO> quizzes = await query
            .Skip(skip)
            .Take(Math.Min(Constants.BATCH_SIZE, Constants.MAX_QUIZZES - skip))
            .Select(q => new FeaturedQuizDTO
            {
                QuizId = q.Id,
                QuizName = q.Name,
                CategoryName = q.Category.CategoryName,
                DifficultyLevel = q.DifficultyLevel.Name,
                TotalAttempts = q.QuizAttempteds.Count(),
                Rating = q.Rating,
                IsAttempted = q.QuizAttempteds.Any(qa => qa.UserId == UserId && qa.QuizId == q.Id)
            })
            .ToListAsync();

        return new FeaturedQuizListDTO
        {
            Quizzes = quizzes,
            HasMore = skip + quizzes.Count < totalToServe
        };
    }

    public async Task<List<BattleRequestDTO>> GetBattleRequests()
    {
        DateTime now = DateTime.UtcNow;

        IQueryable<BattleRequest> query = _battleRequestRepository
            .GetQueryableInclude(
                br => br.Sender,
                br => br.Battle,
                br => br.Battle.Quiz,
                br => br.Battle.Quiz.Category,
                br => br.Battle.Quiz.DifficultyLevel
            )
            .Where(br => br.ReceiverId == UserId
                    && !br.IsDeleted
                    && br.Status == (int)BattleRequestStatus.Pending
                    && !br.Sender.IsDeleted
                    && !br.Battle.IsDeleted
                    && (
                        (!br.Battle.BattleTimeLimited)
                        || (br.Battle.BattleTimeLimited
                            && br.Battle.StartDate <= now
                            && br.Battle.EndDate >= now)
                        )
            )
            .OrderByDescending(br => br.SendingDate);

        List<BattleRequestDTO> result = await query.Select(br => new BattleRequestDTO
        {
            RequestId = br.Id,
            SenderId = br.Sender.Id,
            SenderUserName = br.Sender.UserName,
            SenderProfilePic = br.Sender.ProfilePic,
            SenderFullName = br.Sender.FullName,
            BattleId = br.Battle.Id,
            BattleName = br.Battle.Quiz.Name,
            BattleCategory = br.Battle.Quiz.Category.CategoryName,
            BattleDifficulty = br.Battle.Quiz.DifficultyLevel.Name,
            SendingDate = br.SendingDate,
        }).ToListAsync();

        foreach (BattleRequestDTO item in result)
        {
            item.TimeAgo = GetTimeAgo(item.SendingDate);
        }

        return result;
    }

    public async Task<bool> UpdateBattleRequestStatus(BattleRequestActionDTO dto)
    {
        // status: 1 = Accept, 2 = Reject

        BattleRequest? request = await _battleRequestRepository.GetAsync(br => br.Id == dto.RequestId && !br.IsDeleted);

        if (request == null)
            throw new AppException(string.Format(Constants.BATTLE_REQUEST_NOT_FOUND, dto.RequestId), StatusCodes.Status404NotFound);

        request.Status = dto.Status;
        request.ModifiedDate = DateTime.UtcNow;
        request.ModifiedBy = UserId;

        await _battleRequestRepository.UpdateAsync(request);
        return true;
    }

    public async Task<RankProgressDTO> GetRankProgressAsync()
    {
        RawRankProgressDTO raw = await _sqlQueryRepository.SqlQuerySingleAsync<RawRankProgressDTO>(string.Format(SqlConstants.GET_RANK_PROGRESS, UserId));

        return _mapper.Map<RankProgressDTO>(raw);
    }

    private static string GetTimeAgo(DateTime sendingDate)
    {
        TimeSpan timeSpan = DateTime.UtcNow - sendingDate.ToUniversalTime();

        if (timeSpan.TotalSeconds < 5)
            return Constants.JUST_NOW;
        if (timeSpan.TotalSeconds < 60)
            return string.Format(Constants.SECONDS_AGO, timeSpan.Seconds);
        if (timeSpan.TotalMinutes < 60)
            return string.Format(Constants.MINUTES_AGO, timeSpan.Minutes);
        if (timeSpan.TotalHours < 24)
            return string.Format(Constants.HOURS_AGO, timeSpan.Hours);
        if (timeSpan.TotalDays < 30)
            return string.Format(Constants.DAYS_AGO, timeSpan.Days);
        if (timeSpan.TotalDays < 365)
            return string.Format(Constants.MONTHS_AGO, (int)(timeSpan.TotalDays / 30));

        return string.Format(Constants.YEARS_AGO, (int)(timeSpan.TotalDays / 365));
    }
}
