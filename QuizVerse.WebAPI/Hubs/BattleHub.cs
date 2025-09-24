using Microsoft.AspNetCore.SignalR;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Helper;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.WebAPI.Hubs;

public class BattleHub(
    IBattleMatchmakingService _battleMatchmakingService
) : Hub
{
    private const string BattleIdKey = "BattleId";

    public async Task StartMatchmaking(int battleId)
    {
        int userId = Context.User?.GetUserId()
            ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

        Context.Items[BattleIdKey] = battleId;

        MatchmakingResultDTO result = await _battleMatchmakingService.StartMatchmaking(battleId, userId, Context.ConnectionId);

        if (result.IsMatched)
        {
            await Clients.Client(result.Player!.ConnectionId).SendAsync("MatchFound", result.OpponentProfile);
            await Clients.Client(result.Opponent!.ConnectionId).SendAsync("MatchFound", result.PlayerProfile);
        }
        else
        {
            await Clients.Client(Context.ConnectionId).SendAsync("Searching");
        }
    }

    public Task CancelMatchmaking(int battleId)
    {
        int userId = Context.User?.GetUserId()
            ?? throw new UnauthorizedAccessException(Constants.UNAUTHORIZED_USER);

        _battleMatchmakingService.CancelMatchmaking(battleId, userId);
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        int? userId = Context.User?.GetUserId();
        if (userId.HasValue && Context.Items.TryGetValue(BattleIdKey, out var battleIdObj) && battleIdObj is int battleId)
        {
            _battleMatchmakingService.CancelMatchmaking(battleId, userId.Value);
        }

        return base.OnDisconnectedAsync(exception);
    }
}
