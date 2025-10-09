using Microsoft.AspNetCore.SignalR;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.WebAPI.Hubs;
using static QuizVerse.Infrastructure.Common.Constants;

namespace QuizVerse.WebAPI.Notifications;

public class SignalRNotificationService(
    IHubContext<BattleHub> _hubContext,
    ILogger<SignalRNotificationService> _logger
) : INotificationService
{
    public async Task SendBattleRequestAsync(int receiverUserId, BattleRequestDTO battleRequestDTO)
    {
        try
        {
            await _hubContext.Clients.User(receiverUserId.ToString())
                .SendAsync(SignalRMethods.RECEIVE_BATTLE_REQUEST, battleRequestDTO);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, FAILED_TO_SEND_BATTLE_REQUEST, receiverUserId);
        }
    }
}
