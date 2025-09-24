using QuizVerse.Infrastructure.DTOs;

namespace QuizVerse.Infrastructure.Interface;

public interface IMatchmakingQueueRepository
{
    void AddPlayer(int battleId, MatchmakingPlayerDTO player);
    MatchmakingPlayerDTO? FindMatch(int battleId, MatchmakingPlayerDTO newPlayer);
    void RemovePlayer(int quizId, int userId);
}
