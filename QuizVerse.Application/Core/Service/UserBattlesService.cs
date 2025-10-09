using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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
    IGenericRepository<Domain.Entities.BattleStatus> _battleStatusReposiory,
    IGenericRepository<QuizPlayStatus> _quizPlayStatusRepository,
    IGenericRepository<BattleList> _battleListRepository,
    IGenericRepository<User> _userRepository,
    IGenericRepository<BattleRequest> _battleRequestRepository,
    IHttpContextAccessor httpContextAccessor,
    ISqlQueryRepository sqlQueryRepository,
    IMapper mapper,
    INotificationService _notificationService
    ) : IUserBattlesService
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
    public async Task<UserBattleHistoryResponseDto> GetUserBattleHistory(UserBattleHistoryRequestDto dto)
    {
        var query = string.Format(SqlConstants.GET_USER_BATTLES_HISTORY_QUERY_TEMPLATE,
                                  SqlConstants.GET_USER_BATTLES_HISTORY_FUNCTION);

        var parameters = new NpgsqlParameter[]
        {
        new("p_user_id", NpgsqlDbType.Integer) { Value = UserId },
        new("p_status_draw", NpgsqlDbType.Integer) { Value = (int)Infrastructure.Enums.BattleStatus.Draw },
        new("p_status_completed", NpgsqlDbType.Integer) { Value = (int)Infrastructure.Enums.BattleStatus.Completed },
        new("p_batch_number", NpgsqlDbType.Integer) { Value = dto.BatchNumber },
        new("p_filter_by", NpgsqlDbType.Integer) { Value = dto.FilterBy.HasValue ? (int)dto.FilterBy.Value : DBNull.Value },
        new("p_time_filter_by", NpgsqlDbType.Integer) { Value = dto.TimeFilterBy.HasValue ? (int)dto.TimeFilterBy : DBNull.Value }
        };

        var rawResult = await sqlQueryRepository
            .SqlQuerySingleAsync<UserBattleHistoryRawResult>(query, parameters);

        //Map (HasMore + raw JSON → List<UserBattleHistoryDto>)
        var response = mapper.Map<UserBattleHistoryResponseDto>(rawResult);

        // Map for ToTitleCase
        response.Battles = [.. response.Battles.Select(mapper.Map<UserBattleHistoryDto>)];

        return response;
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

        bool receiverInBattle = await _battleStatusReposiory.Exists(bs =>
            !bs.IsDeleted &&
            bs.BattleStatus1 == (int)Infrastructure.Enums.BattleStatus.Running &&
            (bs.User1Id == receiver.Id || bs.User2Id == receiver.Id)
        );

        if (receiverInBattle)
            throw new AppException(Constants.USER_IN_ACTIVE_BATTLE, StatusCodes.Status409Conflict);

        bool receiverInQuiz = await _quizPlayStatusRepository.Exists(qs =>
            (qs.IsCompleted ?? false) == false && qs.UserId == receiver.Id
        );

        if (receiverInQuiz)
            throw new AppException(Constants.USER_IN_ACTIVE_QUIZ, StatusCodes.Status409Conflict);

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
            br.BattleId == dto.BattleId &&
            activeStatuses.Contains((BattleRequestStatus)br.Status) &&
            !br.IsDeleted
        );

        if (existingChallenge)
            throw new AppException(Constants.ACTIVE_CHALLENGE_EXISTS, StatusCodes.Status400BadRequest);

        BattleRequest battleRequest = mapper.Map<BattleRequest>(dto);
        battleRequest.SenderId = UserId;
        battleRequest.ReceiverId = receiver.Id;

        await _battleRequestRepository.AddAsync(battleRequest);

        var sender = await _userRepository.GetAsync(u => u.Id == UserId && !u.IsDeleted);
        var battle = await _battleListRepository.GetAsync(
            b => b.Id == dto.BattleId && !b.IsDeleted,
            includes: q => q
                .Include(x => x.Quiz).ThenInclude(qz => qz.Category)
                .Include(x => x.Quiz).ThenInclude(qz => qz.DifficultyLevel)
        );

        BattleRequestDTO battleRequestDTO = new()
        {
            RequestId = battleRequest.Id,
            SenderUserName = sender?.UserName!,
            SenderFullName = sender?.FullName!,
            SenderProfilePic = sender?.ProfilePic,
            BattleName = battle?.Quiz.Name,
            BattleCategory = battle?.Quiz.Category?.CategoryName!,
            BattleDifficulty = battle?.Quiz.DifficultyLevel?.Name!,
            SendingDate = DateTime.UtcNow,
            TimeAgo = "just now"
        };

        await _notificationService.SendBattleRequestAsync(receiver.Id, battleRequestDTO);

        return Constants.BATTLE_REQUEST_SENT_SUCCESS;
    }

    public async Task<bool> CheckUserExistence(string userName)
    {
        User? user = await _userRepository.GetAsync(u => u.UserName.Trim() == userName.Trim() && !u.IsDeleted);

        if (user != null)
        {
            if (user.Id == UserId)
                throw new AppException(Constants.SELF_CHALLENGE_NOT_ALLOWED, StatusCodes.Status400BadRequest);
        }
        else
        {
            throw new AppException(Constants.USERNAME_DOES_NOT_EXIST, StatusCodes.Status400BadRequest);
        }
        return true;
    }
    public async Task<List<SearchUserResponseDto>> SearchUsersAsync(string userName, int battleId)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return new List<SearchUserResponseDto>();

        var currentUserId = UserId; // get current user

        // Materialize the users query asynchronously
        var allUsers = await _userRepository
            .GetQueryableInclude(u => u.UserPerformanceDetail!)
            .ToListAsync();

        var filteredUsers = allUsers
            .Where(u => u.Id != currentUserId &&
                        !u.IsDeleted &&
                        u.Status == (int)UserStatus.Active &&
                        u.RoleId != 1 &&
                        u.UserName.Contains(userName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Materialize battle requests asynchronously
        var battleRequests = await _battleRequestRepository
            .GetQueryableInclude()
            .ToListAsync();

        var result = filteredUsers.Select(u =>
        {
            var dto = mapper.Map<SearchUserResponseDto>(u);
            dto.HasRequest = battleRequests.Any(br =>
                br.SenderId == currentUserId && br.ReceiverId == u.Id && br.BattleId == battleId) ;
            return dto;
        }).ToList();

        return result;
    }


    #endregion

    #region Get Battle Result
    public async Task<UserBattleResult> GetBattleResult(int battleId)
    {
        string query = string.Format(SqlConstants.GET_USER_BATTLE_RESULT_TEMPLATE, SqlConstants.GET_USER_BATTLE_RESULT_FUNCTION);
        var parameters = new NpgsqlParameter[]
        {
        new("p_battle_id", NpgsqlDbType.Integer) { Value = battleId },
        new("p_login_user_id", NpgsqlDbType.Integer) { Value = UserId },
        new("p_status_running", NpgsqlDbType.Integer) { Value = (int)Infrastructure.Enums.BattleStatus.Running }
        };

        return mapper.Map<UserBattleResult>(await sqlQueryRepository.SqlQuerySingleAsync<UserBattleResult>(query, parameters));
    }
    #endregion
}
