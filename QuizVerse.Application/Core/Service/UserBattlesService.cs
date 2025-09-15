using AutoMapper;
using Microsoft.AspNetCore.Http;
using Npgsql;
using NpgsqlTypes;
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

public class UserBattlesService(
    IGenericRepository<BattleList> _battleListRepository,
    IGenericRepository<User> _userRepository,
    IGenericRepository<BattleRequest> _battleRequestRepository,
    IHttpContextAccessor httpContextAccessor,
    ISqlQueryRepository sqlQueryRepository,
    IMapper mapper) : IUserBattlesService
{
    public int UserId => httpContextAccessor.HttpContext?.User?.GetUserId() ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

    #region User Available Battles
    public async Task<List<UserAvailableBattleDto>> GetUserAvailableBattles()
    {
        string query = string.Format(SqlConstants.GET_USER_AVAILABLE_BATTLES_QUERY_TEMPLATE,
                                  SqlConstants.GET_USER_AVAILABLE_BATTLES_FUNCTION);

        NpgsqlParameter paramUserId = new("p_user_id", NpgsqlDbType.Integer) { Value = UserId };

        List<UserAvailableBattleDto> rawResult = await sqlQueryRepository.SqlQueryListAsync<UserAvailableBattleDto>(query, paramUserId);

        return rawResult;
    }
    #endregion

    #region User Recent Battles
    public async Task<List<UserRecentBattleDto>> GetUserRecentBattles()
    {
        var query = string.Format(SqlConstants.GET_USER_RECENT_BATTLES_QUERY_TEMPLATE,
                                  SqlConstants.GET_USER_RECENT_BATTLES_FUNCTION);

        var parameters = new NpgsqlParameter[]
        {
        new("p_user_id", NpgsqlDbType.Integer) { Value = UserId },
        new("p_status_draw", NpgsqlDbType.Integer) { Value = (int)Infrastructure.Enums.BattleStatus.Draw },
        new("p_status_completed", NpgsqlDbType.Integer) { Value = (int)Infrastructure.Enums.BattleStatus.Completed }
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
            new("p_completed_battle_status", NpgsqlDbType.Integer) { Value = (int)Infrastructure.Enums.BattleStatus.Completed },
            new("p_draw_battle_status", NpgsqlDbType.Integer) {Value = (int)Infrastructure.Enums.BattleStatus.Draw},
            new("p_active_user_status", NpgsqlDbType.Integer) {Value = (int)UserStatus.Active},
            new("p_logged_in_user_id", NpgsqlDbType.Integer) { Value = UserId}
        };

        return mapper.Map<List<UserBattleLeaderboardData>>
            (await sqlQueryRepository.SqlQueryListAsync<UserBattleLeaderboardData>(query, parameters));
    }
    #endregion

    #region Battle Request Management
    public async Task<string> SendBattleRequest(SendBattleRequestDTO dto)
    {
        bool battleExists = await _battleListRepository.Exists(b => b.Id == dto.BattleId && !b.IsDeleted);
        if (!battleExists)
            throw new AppException(Constants.BATTLE_NOT_FOUND, StatusCodes.Status404NotFound);

        User? receiver = await _userRepository.GetAsync(u => u.UserName == dto.ReceiverUsername && !u.IsDeleted);
        if (receiver == null)
            throw new AppException(Constants.USER_NOT_FOUND_MESSAGE, StatusCodes.Status404NotFound);

        if (UserId == receiver.Id)
            throw new AppException(Constants.SELF_CHALLENGE_NOT_ALLOWED, StatusCodes.Status400BadRequest);

        bool anyAccepted = await _battleRequestRepository.Exists(br =>
            br.SenderId == UserId &&
            br.BattleId == dto.BattleId &&
            br.Status == (int)BattleRequestStatus.Accepted &&
            !br.IsDeleted
        );

        if (anyAccepted)
            throw new AppException(Constants.BATTLE_ALREADY_ACCEPTED, StatusCodes.Status400BadRequest);

        BattleRequestStatus[] activeStatuses = [BattleRequestStatus.Pending, BattleRequestStatus.Accepted];
        bool existingChallenge = await _battleRequestRepository.Exists(br =>
            br.SenderId == UserId &&
            br.ReceiverId == receiver.Id &&
            activeStatuses.Contains((BattleRequestStatus)br.Status) &&
            !br.IsDeleted
        );

        if (existingChallenge)
            throw new AppException(Constants.ACTIVE_CHALLENGE_EXISTS, StatusCodes.Status400BadRequest);

        BattleRequest battleRequest = mapper.Map<BattleRequest>(dto);
        battleRequest.SenderId = UserId;
        battleRequest.ReceiverId = receiver.Id;

        await _battleRequestRepository.AddAsync(battleRequest);

        return Constants.BATTLE_REQUEST_SENT_SUCCESS;
    }
    #endregion
}
