using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.WebAPI.Hubs;
using QuizVerse.WebAPI.Notifications;
using Xunit;
using static QuizVerse.Infrastructure.Common.Constants;

namespace QuizVerse.UnitTests.Notifications;

public class SignalRNotificationServiceTests
{
    private readonly Mock<IHubContext<BattleHub>> _mockHubContext;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<IHubClients> _mockClients;
    private readonly Mock<ILogger<SignalRNotificationService>> _mockLogger;
    private readonly SignalRNotificationService _service;

    public SignalRNotificationServiceTests()
    {
        _mockHubContext = new Mock<IHubContext<BattleHub>>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockClients = new Mock<IHubClients>();
        _mockLogger = new Mock<ILogger<SignalRNotificationService>>();

        // Setup HubContext -> Clients -> User
        _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
        _mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(_mockClientProxy.Object);

        _service = new SignalRNotificationService(_mockHubContext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SendBattleRequestAsync_ShouldCallSendAsync_WithCorrectParameters()
    {
        var receiverUserId = 123;
        var dto = new BattleRequestDTO
        {
            RequestId = 1,
            SenderUserName = "sender",
            SenderFullName = "Sender Name",
            BattleName = "Battle 1",
            BattleCategory = "Category",
            BattleDifficulty = "Easy",
            SendingDate = DateTime.UtcNow,
            TimeAgo = "just now"
        };

        await _service.SendBattleRequestAsync(receiverUserId, dto);

        _mockClients.Verify(c => c.User(receiverUserId.ToString()), Times.Once);
        _mockClientProxy.Verify(p => p.SendCoreAsync(
            SignalRMethods.RECEIVE_BATTLE_REQUEST,
            It.Is<object?[]>(args => args.Length == 1 && args[0] == dto),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SendBattleRequestAsync_ShouldLogError_WhenSendAsyncThrowsException()
    {
        var receiverUserId = 123;
        var dto = new BattleRequestDTO { RequestId = 1 };
        var exception = new Exception("SignalR failure");

        _mockClientProxy
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        await _service.SendBattleRequestAsync(receiverUserId, dto);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to send battle request")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SendBattleRequestAsync_ShouldHandleMultipleCalls()
    {
        var dto1 = new BattleRequestDTO { RequestId = 1 };
        var dto2 = new BattleRequestDTO { RequestId = 2 };
        var receiverUserId = 456;

        await _service.SendBattleRequestAsync(receiverUserId, dto1);
        await _service.SendBattleRequestAsync(receiverUserId, dto2);

        _mockClients.Verify(c => c.User(receiverUserId.ToString()), Times.Exactly(2));
        _mockClientProxy.Verify(p => p.SendCoreAsync(
            SignalRMethods.RECEIVE_BATTLE_REQUEST,
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()
        ), Times.Exactly(2));
    }

}
