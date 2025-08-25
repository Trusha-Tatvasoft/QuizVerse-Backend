using AutoMapper;
using Microsoft.AspNetCore.Http;
using Npgsql;
using NpgsqlTypes;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class LeaderboardService(IGenericRepository<UserPerformanceDetail> _leaderboardRepository, IHttpContextAccessor _httpContextAccessor, IMapper _mapper, ISqlQueryRepository _sqlQueryRepository) : ILeaderboardService
{
    private int UserId => _httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new Exception(Constants.USER_NOT_AUTHENTICATED_MESSAGE);
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

        var parameters = new NpgsqlParameter[]
        {
        new("p_user_id", NpgsqlDbType.Integer) { Value = (object?)UserId ?? DBNull.Value }
        };

        // --- FETCH DATA ---
        List<RawLeaderboardGlobalRankingDto> leaderboardRaw =
            await _sqlQueryRepository.SqlQueryListAsync<RawLeaderboardGlobalRankingDto>(query, parameters);

        // --- MAP TO RESPONSE DTO ---
        return _mapper.Map<List<LeaderboardGlobalRankingResponseDto>>(leaderboardRaw);
    }
}
