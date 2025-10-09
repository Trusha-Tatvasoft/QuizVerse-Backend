using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class UserActivityCheckerService(
    IGenericRepository<BattleStatus> _battleStatusRepository,
    IGenericRepository<QuizPlayStatus> _quizPlayStatusRepository
) : IUserActivityCheckerService
{
    public async Task<bool> IsUserBusy(int userId)
    {
        bool inBattle = await _battleStatusRepository.Exists(bs =>
            !bs.IsDeleted &&
            bs.BattleStatus1 == (int)Infrastructure.Enums.BattleStatus.Running &&
            (bs.User1Id == userId || bs.User2Id == userId)
        );

        if (inBattle)
            return true;

        bool inQuiz = await _quizPlayStatusRepository.Exists(qs =>
            (qs.IsCompleted ?? false) == false && qs.UserId == userId
        );

        return inQuiz;
    }
}
