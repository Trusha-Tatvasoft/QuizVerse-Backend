using Microsoft.EntityFrameworkCore;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class BattleMatchmakingService(
    IMatchmakingQueueRepository _matchmakingQueueRepository,
    IGenericRepository<User> _userRepo,
    IGenericRepository<BattleStatus> _battleStatusRepository,
    IGenericRepository<BattleResult> _battleResultRepository
) : IBattleMatchmakingService
{
    public async Task<MatchmakingResultDTO> StartMatchmaking(int battleId, int userId, string connectionId)
    {
        PlayerProfileDTO? playerProfile = await GetPlayerProfile(userId);

        MatchmakingPlayerDTO player = new()
        {
            ConnectionId = connectionId,
            UserId = userId,
            BattleId = battleId,
            EnqueuedAt = DateTime.UtcNow
        };

        MatchmakingPlayerDTO? opponent = FindOpponent(player);

        if (opponent != null)
        {
            PlayerProfileDTO? opponentProfile = await GetPlayerProfile(opponent.UserId);

            return new MatchmakingResultDTO
            {
                IsMatched = true,
                Player = player,
                Opponent = opponent,
                PlayerProfile = playerProfile!,
                OpponentProfile = opponentProfile!
            };
        }

        return new MatchmakingResultDTO { IsMatched = false };
    }

    public async Task<MatchmakingResultDTO?> StartFriendBattle(int battleId, int senderUserId, int receiverUserId)
    {
        PlayerProfileDTO? senderProfile = await GetPlayerProfile(senderUserId);
        PlayerProfileDTO? receiverProfile = await GetPlayerProfile(receiverUserId);

        if (senderProfile == null || receiverProfile == null)
            return null;

        MatchmakingPlayerDTO sender = new()
        {
            ConnectionId = string.Empty,
            UserId = senderUserId,
            BattleId = battleId,
            EnqueuedAt = DateTime.UtcNow
        };

        MatchmakingPlayerDTO receiver = new()
        {
            ConnectionId = string.Empty,
            UserId = receiverUserId,
            BattleId = battleId,
            EnqueuedAt = DateTime.UtcNow
        };

        MatchmakingResultDTO result = new()
        {
            IsMatched = true,
            Player = sender,
            Opponent = receiver,
            PlayerProfile = senderProfile,
            OpponentProfile = receiverProfile
        };

        return result;
    }

    public MatchmakingPlayerDTO? FindOpponent(MatchmakingPlayerDTO player)
    {
        _matchmakingQueueRepository.AddPlayer(player.BattleId, player);

        return _matchmakingQueueRepository.FindMatch(player.BattleId, player);
    }

    public async Task<double> GetUserWinRate(int userId)
    {
        var query = _battleStatusRepository.GetQueryableInclude()
            .Where(bs => !bs.IsDeleted
                         && (bs.User1Id == userId || bs.User2Id == userId)
                         && bs.BattleStatus1 != (int)Infrastructure.Enums.BattleStatus.Running)
            .Join(_battleResultRepository.GetQueryableInclude(),
                  bs => bs.Id,
                  br => br.BattleStatus,
                  (bs, br) => new { bs.BattleStatus1, br.WinnerId });

        int total = await query.CountAsync(x =>
            x.BattleStatus1 == (int)Infrastructure.Enums.BattleStatus.Completed ||
            x.BattleStatus1 == (int)Infrastructure.Enums.BattleStatus.Draw);

        if (total == 0) return 0;

        int wins = await query.CountAsync(x => x.WinnerId == userId);

        return Math.Round(wins * 100.0 / total, 2);
    }

    public virtual async Task<PlayerProfileDTO?> GetPlayerProfile(int userId)
    {
        PlayerProfileDTO? user = await _userRepo.GetQueryableInclude(u => u.UserPerformanceDetail!)
        .Where(u => u.Id == userId && !u.IsDeleted)
        .Select(u => new PlayerProfileDTO
        {
            UserId = u.Id,
            UserName = u.UserName,
            FullName = u.FullName,
            CurrentLevel = (int?)u.UserPerformanceDetail!.CurrentLevel ?? 1,
            ProfilePic = u.ProfilePic ?? string.Empty
        })
        .FirstOrDefaultAsync();

        if (user != null)
        {
            user.WinRate = await GetUserWinRate(userId);
        }

        return user;
    }

    public void CancelMatchmaking(int battleId, int userId)
    {
        _matchmakingQueueRepository.RemovePlayer(battleId, userId);
    }
}
