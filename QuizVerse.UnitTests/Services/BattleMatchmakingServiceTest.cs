using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.UnitTests.Services;

public class BattleMatchmakingServiceTest
{
    private readonly Mock<IMatchmakingQueueRepository> _matchmakingQueueRepoMock;
    private readonly Mock<IGenericRepository<User>> _userRepoMock;
    private readonly Mock<IGenericRepository<BattleStatus>> _battleStatusRepoMock;
    private readonly Mock<IGenericRepository<BattleResult>> _battleResultRepoMock;
    private readonly BattleMatchmakingService _service;

    public BattleMatchmakingServiceTest()
    {
        _matchmakingQueueRepoMock = new Mock<IMatchmakingQueueRepository>();
        _userRepoMock = new Mock<IGenericRepository<User>>();
        _battleStatusRepoMock = new Mock<IGenericRepository<BattleStatus>>();
        _battleResultRepoMock = new Mock<IGenericRepository<BattleResult>>();

        _service = new BattleMatchmakingService(
            _matchmakingQueueRepoMock.Object,
            _userRepoMock.Object,
            _battleStatusRepoMock.Object,
            _battleResultRepoMock.Object
        );
    }
}
