using AutoMapper;
using Microsoft.AspNetCore.Http;
using Npgsql;
using NpgsqlTypes;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class UserBattlesService(IHttpContextAccessor httpContextAccessor, ISqlQueryRepository sqlQueryRepository, IMapper mapper) : IUserBattlesService
{
    public int UserId => httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

    #region User Recent Battles
    public async Task<List<UserRecentBattleDto>> GetUserRecentBattles()
    {
        var query = string.Format(SqlConstants.GET_USER_RECENT_BATTLES_QUERY_TEMPLATE,
                                  SqlConstants.GET_USER_RECENT_BATTLES_FUNCTION);

        var parameters = new NpgsqlParameter[]
        {
        new("p_user_id", NpgsqlDbType.Integer) { Value = UserId },
        new("p_status_draw", NpgsqlDbType.Integer) { Value = (int)BattleStatus.Draw },
        new("p_status_completed", NpgsqlDbType.Integer) { Value = (int)BattleStatus.Completed }
        };

        var rawResult = await sqlQueryRepository.SqlQueryListAsync<UserRecentBattleDto>(query, parameters);

        return mapper.Map<List<UserRecentBattleDto>>(rawResult);
    }
    #endregion

    #region Battle Leaderboard Data 
    public async Task<List<UserBattleLeaderboardData>> GetBattleLeaderboardList()
    {
        string query = string.Format(
           SqlConstants.GET_USER_BATTLE_LEADERBOARD_LIST_TEMPLATE,
           SqlConstants.GET_USER_BATTLE_LEADERBOARD_LIST_FUNCTION
       );

        var parameters = new NpgsqlParameter[]
        {
            new("p_completed_battle_status", NpgsqlDbType.Integer) { Value = (int)BattleStatus.Completed },
            new("p_draw_battle_status", NpgsqlDbType.Integer) {Value = (int)BattleStatus.Draw},
            new("p_active_user_status", NpgsqlDbType.Integer) {Value = (int)UserStatus.Active},
            new("p_logged_in_user_id", NpgsqlDbType.Integer) { Value = UserId}
        };

        return mapper.Map<List<UserBattleLeaderboardData>>
            (await sqlQueryRepository.SqlQueryListAsync<UserBattleLeaderboardData>(query, parameters));
    }
    #endregion
}
