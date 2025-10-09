namespace QuizVerse.Application.Core.Interface;

public interface IUserActivityCheckerService
{
    Task<bool> IsUserBusy(int userId);
}
