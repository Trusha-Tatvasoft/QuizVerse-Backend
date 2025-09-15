using AutoMapper;
using Microsoft.AspNetCore.Http;
using Npgsql;
using NpgsqlTypes;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class LeaderboardService(IGenericRepository<UserPerformanceDetail> _leaderboardRepository, IGenericRepository<QuizCategory> _quizCategoryRepository, IHttpContextAccessor _httpContextAccessor, IMapper _mapper, ISqlQueryRepository _sqlQueryRepository, IGenericRepository<QuizAttempted> _quizAttemptedRepository, IGenericRepository<BattleStatus> _battleStatusRepository, IMemoryCacheService _cacheService) : ILeaderboardService
{
    private int UserId => _httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);
    public async Task<UserPerformanceResponseDto> GetUserLeaderboardStats()
    {
        UserPerformanceDetail userPerformanceDetail = await _leaderboardRepository.GetAsync(u => u.UserId == UserId) ?? new UserPerformanceDetail();
        UserPerformanceResponseDto userPerformanceDto = _mapper.Map<UserPerformanceResponseDto>(userPerformanceDetail);
        return userPerformanceDto;
    }

    public async Task<List<LeaderboardGlobalRankingResponseDto>> GetLeaderboardGlobalRanking()
    {
        // --- QUERY ---
        string query = string.Format(
            SqlConstants.GET_LEADERBOARD_QUERY_TEMPLATE,   // e.g. "SELECT * FROM {0}(@p_user_id)"
            SqlConstants.GET_GLOBAL_LEADERBOARD_FUNCTION  // e.g. "get_leaderboard_global_rankings"
        );

        NpgsqlParameter[] parameters =
        [
            new("p_user_id", NpgsqlDbType.Integer) { Value = (object?)UserId ?? DBNull.Value }
        ];

        // --- FETCH DATA ---
        List<RawLeaderboardGlobalRankingDto> leaderboardRaw =
            await _sqlQueryRepository.SqlQueryListAsync<RawLeaderboardGlobalRankingDto>(query, parameters);

        // --- MAP TO RESPONSE DTO ---
        return _mapper.Map<List<LeaderboardGlobalRankingResponseDto>>(leaderboardRaw);
    }

    public async Task<List<WeeklyLeaderBoardResponseDto>> GetWeeklyLeaderboardRanking()
    {
        string query = string.Format(SqlConstants.GET_WEEKLY_LEADERBOARD_QUERY_TEMPLATE, SqlConstants.GET_WEEKLY_LEADERBOARD_FUNCTION);

        var parameters = new NpgsqlParameter[]
        {
            new("p_user_id", NpgsqlDbType.Integer) { Value = (object?)UserId ?? DBNull.Value }
        };

        List<WeeklyLeaderBoardResponseDto> weeklyLeaderBoards =
            await _sqlQueryRepository.SqlQueryListAsync<WeeklyLeaderBoardResponseDto>(query, parameters);

        return weeklyLeaderBoards;
    }

    public async Task<List<CategoryWiseLeaderBoardResponseDto>> GetQuizCategoryWiseLeaderboardRanking(int categoryId)
    {
        string query = string.Format(SqlConstants.GET_CATEGORY_WISE_LEADERBOARD_QUERY_TEMPLATE, SqlConstants.GET_CATEGORY_WISE_LEADERBOARD_FUNCTION);

        QuizCategory? category = await _quizCategoryRepository.GetAsync(c => c.Id == categoryId);
        if (category == null)
        {
            throw new AppException(Constants.QUIZ_CATEGORY_NOT_FOUND_MESSAGE);
        }

        var parameters = new NpgsqlParameter[]
        {
            new("p_user_id", NpgsqlDbType.Integer) { Value = (object?)UserId ?? DBNull.Value },
            new("p_category_id", NpgsqlDbType.Integer) { Value = (object?)categoryId ?? DBNull.Value }
        };

        List<CategoryWiseLeaderBoardResponseDto> categoryWiseLeaderBoards =
            await _sqlQueryRepository.SqlQueryListAsync<CategoryWiseLeaderBoardResponseDto>(query, parameters);

        return categoryWiseLeaderBoards;
    }

    public async Task<List<MonthlyChampionsResponseDto>> GetMonthlyChampions(int month, int year)
    {
        string query = string.Format(SqlConstants.GET_MONTHLY_CHAMPIONS_QUERY_TEMPLATE, SqlConstants.GET_MONTHLY_CHAMPIONS_FUNCTION);

        if(month < 1 || month > 12)
        {
            throw new AppException(Constants.INVALID_MONTH_MESSAGE,400);
        }

        if(year < 2023 || year > DateTime.Now.Year)
        {
            throw new AppException(Constants.INVALID_YEAR_MESSAGE,400);
        }

        if(year == DateTime.Now.Year && month > DateTime.Now.Month)
        {
            throw new AppException(Constants.INVALID_MONTH_YEAR_COMBINATION_MESSAGE,400);
        }

        var parameters = new NpgsqlParameter[]
        {
            new("p_user_id", NpgsqlDbType.Integer) { Value = (object?)UserId ?? DBNull.Value },
            new("p_month", NpgsqlDbType.Integer) { Value = (object?)month ?? DBNull.Value },
            new("p_year", NpgsqlDbType.Integer) { Value = (object?)year ?? DBNull.Value }
        };

        List<MonthlyChampionsResponseDto> monthlyChampions =
            await _sqlQueryRepository.SqlQueryListAsync<MonthlyChampionsResponseDto>(query, parameters);

        return monthlyChampions;
    }
    
    public List<CommonListDropDownDto> GetAvailableYears()
    {
        return _cacheService.GetOrSet("AvailableYears", () =>
        {
            IQueryable<int> quizYears = _quizAttemptedRepository.GetQueryableInclude()
                .Select(q => q.CreatedDate.Year);

            IQueryable<int> battleYears = _battleStatusRepository.GetQueryableInclude()
                .Select(b => b.CreatedDate.Year);

            List<int> distinctYears = [.. quizYears
                .Union(battleYears)
                .Distinct()
                .OrderByDescending(y => y)];

            return distinctYears
                .Select(y => new CommonListDropDownDto
                {
                    Id = y,
                    Name = y.ToString()
                })
                .ToList();
        });
    }

    public void ClearAvailableYearsCache()
    {
        _cacheService.Clear("AvailableYears");
    }

    public List<CommonListDropDownDto> GetAvailableMonthsByYear(int year)
    {
        return _cacheService.GetOrSet($"AvailableMonths_{year}", () =>
        {
            IQueryable<int> quizMonths = _quizAttemptedRepository.GetQueryableInclude()
                .Where(q => q.CreatedDate.Year == year)
                .Select(q => q.CreatedDate.Month);

            IQueryable<int> battleMonths = _battleStatusRepository.GetQueryableInclude()
                .Where(b => b.CreatedDate.Year == year)
                .Select(b => b.CreatedDate.Month);

            List<int> distinctMonths = [.. quizMonths
                .Union(battleMonths)
                .Distinct()
                .OrderBy(m => m)];

            return distinctMonths
                .Select(m => new CommonListDropDownDto
                {
                    Id = m,
                    Name = new DateTime(2000, m, 1).ToString("MMMM")
                })
                .ToList();
        });
    }

    public void ClearAvailableMonthsCache(int year)
    {
        _cacheService.Clear($"AvailableMonths_{year}");
    }
}
