using QuizVerse.Infrastructure.DTOs.ResponseDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface INotificationService
{
    Task SendBattleRequestAsync(int receiverUserId, BattleRequestDTO battleRequestDTO);
}
