using System.Collections.Concurrent;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Infrastructure.Repository;

public class MatchmakingQueueRepository : IMatchmakingQueueRepository
{
    private static readonly ConcurrentDictionary<int, List<MatchmakingPlayerDTO>> WaitingPlayers = new();

    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

    public void AddPlayer(int battleId, MatchmakingPlayerDTO player)
    {
        List<MatchmakingPlayerDTO> list = WaitingPlayers.GetOrAdd(battleId, _ => []);
        lock (list)
        {
            list.RemoveAll(p => DateTime.UtcNow - p.EnqueuedAt > _timeout);

            if (!list.Any(p => p.UserId == player.UserId))
                list.Add(player);
        }
    }

    public MatchmakingPlayerDTO? FindMatch(int battleId, MatchmakingPlayerDTO newPlayer)
    {
        if (!WaitingPlayers.TryGetValue(battleId, out var players)) return null;

        lock (players)
        {
            players.RemoveAll(p => DateTime.UtcNow - p.EnqueuedAt > _timeout);

            MatchmakingPlayerDTO? opponent = players.FirstOrDefault(p => p.UserId != newPlayer.UserId );

            if (opponent != null)
            {
                players.RemoveAll(p => p.UserId == opponent.UserId || p.UserId == newPlayer.UserId);
                return opponent;
            }
        }

        return null;
    }

    public void RemovePlayer(int battleId, int userId)
    {
        if (WaitingPlayers.TryGetValue(battleId, out var players))
        {
            lock (players)
            {
                players.RemoveAll(p => p.UserId == userId);
            }
        }
    }
}
