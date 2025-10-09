using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner = inner;

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object Execute(Expression expression)
    {
        return _inner.Execute(expression)!;
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: [typeof(Expression)])?
            .MakeGenericMethod(resultType)
            .Invoke(this, [expression]);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))?
            .MakeGenericMethod(resultType)!
            .Invoke(null, [executionResult])!;
    }
}

public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

public class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner = inner;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }

    public T Current => _inner.Current;
}

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

    [Fact]
    public async Task StartMatchmaking_NoOpponent_ReturnsIsMatchedFalse()
    {
        int battleId = 1, userId = 10;
        string connectionId = "conn-1";

        var userList = new List<User>
        {
            new() { Id = userId, UserName = "User1", FullName = "Test User", IsDeleted = false,
                UserPerformanceDetail = new UserPerformanceDetail { CurrentLevel = 2 } }
        };

        _userRepoMock.Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<User, object>>>()))
            .Returns(new TestAsyncEnumerable<User>(userList));

        _battleStatusRepoMock.Setup(r => r.GetQueryableInclude())
            .Returns(new TestAsyncEnumerable<BattleStatus>([]));

        _battleResultRepoMock.Setup(r => r.GetQueryableInclude())
            .Returns(new TestAsyncEnumerable<BattleResult>([]));

        _matchmakingQueueRepoMock.Setup(r => r.FindMatch(It.IsAny<int>(), It.IsAny<MatchmakingPlayerDTO>()))
            .Returns((MatchmakingPlayerDTO?)null);

        var result = await _service.StartMatchmaking(battleId, userId, connectionId);

        Assert.False(result.IsMatched);
    }

    [Fact]
    public async Task StartMatchmaking_WithOpponent_ReturnsIsMatchedTrue()
    {
        int battleId = 1, userId = 10, oppId = 20;
        string connectionId = "conn-1";

        var userList = new List<User>
        {
            new() { Id = userId, UserName = "User1", FullName = "Test User", IsDeleted = false,
                UserPerformanceDetail = new UserPerformanceDetail { CurrentLevel = 2 } },
            new() { Id = oppId, UserName = "User2", FullName = "Opponent User", IsDeleted = false,
                UserPerformanceDetail = new UserPerformanceDetail { CurrentLevel = 3 } }
        };

        _userRepoMock.Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<User, object>>>()))
            .Returns(new TestAsyncEnumerable<User>(userList));

        _battleStatusRepoMock.Setup(r => r.GetQueryableInclude())
            .Returns(new TestAsyncEnumerable<BattleStatus>([]));

        _battleResultRepoMock.Setup(r => r.GetQueryableInclude())
            .Returns(new TestAsyncEnumerable<BattleResult>([]));

        var opponent = new MatchmakingPlayerDTO
        {
            UserId = oppId,
            BattleId = battleId,
            ConnectionId = "opp-conn",
        };

        _matchmakingQueueRepoMock.Setup(r => r.FindMatch(It.IsAny<int>(), It.IsAny<MatchmakingPlayerDTO>()))
            .Returns(opponent);

        var result = await _service.StartMatchmaking(battleId, userId, connectionId);

        Assert.True(result.IsMatched);
        Assert.NotNull(result.Opponent);
        Assert.Equal(oppId, result.Opponent!.UserId);
        Assert.Equal(userId, result.Player!.UserId);
    }

    [Fact]
    public void FindOpponent_CallsAddPlayerAndFindMatch()
    {
        var player = new MatchmakingPlayerDTO { UserId = 10, BattleId = 1, ConnectionId = "c1" };

        var opponent = new MatchmakingPlayerDTO { UserId = 20, BattleId = 1, ConnectionId = "c2" };

        _matchmakingQueueRepoMock.Setup(r => r.FindMatch(1, player)).Returns(opponent);

        var result = _service.FindOpponent(player);

        _matchmakingQueueRepoMock.Verify(r => r.AddPlayer(1, player), Times.Once);
        _matchmakingQueueRepoMock.Verify(r => r.FindMatch(1, player), Times.Once);
        Assert.Equal(opponent, result);
    }

    [Fact]
    public async Task GetUserWinRate_NoBattles_ReturnsZero()
    {
        var statuses = new List<BattleStatus>();
        var results = new List<BattleResult>();

        _battleStatusRepoMock.Setup(r => r.GetQueryableInclude())
            .Returns(new TestAsyncEnumerable<BattleStatus>(statuses));

        _battleResultRepoMock.Setup(r => r.GetQueryableInclude())
            .Returns(new TestAsyncEnumerable<BattleResult>(results));

        var winRate = await _service.GetUserWinRate(10);

        Assert.Equal(0, winRate);
    }

    [Fact]
    public async Task GetUserWinRate_WithWinsAndLosses_ReturnsCorrectRate()
    {
        int userId = 10;

        var statuses = new List<BattleStatus>
        {
            new() { Id = 1, User1Id = userId, BattleStatus1 = (int)Infrastructure.Enums.BattleStatus.Completed, IsDeleted = false },
            new() { Id = 2, User2Id = userId, BattleStatus1 = (int)Infrastructure.Enums.BattleStatus.Completed, IsDeleted = false }
        };

        var results = new List<BattleResult>
        {
            new() { BattleStatus = 1, WinnerId = userId },
            new() { BattleStatus = 2, WinnerId = 999 }
        };

        _battleStatusRepoMock.Setup(r => r.GetQueryableInclude())
            .Returns(new TestAsyncEnumerable<BattleStatus>(statuses));

        _battleResultRepoMock.Setup(r => r.GetQueryableInclude())
            .Returns(new TestAsyncEnumerable<BattleResult>(results));

        var winRate = await _service.GetUserWinRate(userId);

        Assert.Equal(50.00, winRate);
    }

    [Fact]
    public async Task GetPlayerProfile_UserExists_ReturnsProfileWithWinRate()
    {
        int userId = 10;

        var users = new List<User>
        {
            new() {
                Id = userId,
                UserName = "U1",
                FullName = "Full User",
                ProfilePic = "pic.png",
                UserPerformanceDetail = new UserPerformanceDetail { CurrentLevel = 4 },
                IsDeleted = false
            }
        };

        _userRepoMock.Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<User, object>>>()))
            .Returns(new TestAsyncEnumerable<User>(users));

        _battleStatusRepoMock.Setup(r => r.GetQueryableInclude())
            .Returns(new TestAsyncEnumerable<BattleStatus>([]));

        _battleResultRepoMock.Setup(r => r.GetQueryableInclude())
            .Returns(new TestAsyncEnumerable<BattleResult>([]));

        var profile = await _service.GetPlayerProfile(userId);

        Assert.NotNull(profile);
        Assert.Equal(userId, profile!.UserId);
        Assert.Equal("U1", profile.UserName);
        Assert.Equal(4, profile.CurrentLevel);
        Assert.Equal(0, profile.WinRate);
    }

    [Fact]
    public async Task GetPlayerProfile_UserNotFound_ReturnsNull()
    {
        var users = new List<User>();

        _userRepoMock.Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<User, object>>>()))
            .Returns(new TestAsyncEnumerable<User>(users));

        var profile = await _service.GetPlayerProfile(99);

        Assert.Null(profile);
    }

    [Fact]
    public void CancelMatchmaking_CallsRemovePlayer()
    {
        _service.CancelMatchmaking(1, 10);

        _matchmakingQueueRepoMock.Verify(r => r.RemovePlayer(1, 10), Times.Once);
    }

    [Fact]
    public async Task StartFriendBattle_BothProfilesExist_ReturnsMatchedResult()
    {
        int battleId = 1, senderId = 10, receiverId = 20;

        var senderProfile = new PlayerProfileDTO
        {
            UserId = senderId,
            UserName = "Sender",
            FullName = "Sender Name",
            CurrentLevel = 5,
            WinRate = 60
        };

        var receiverProfile = new PlayerProfileDTO
        {
            UserId = receiverId,
            UserName = "Receiver",
            FullName = "Receiver Name",
            CurrentLevel = 6,
            WinRate = 75
        };

        var serviceMock = new Mock<BattleMatchmakingService>(
            _matchmakingQueueRepoMock.Object,
            _userRepoMock.Object,
            _battleStatusRepoMock.Object,
            _battleResultRepoMock.Object
        );

        serviceMock
            .Setup(s => s.GetPlayerProfile(senderId))
            .ReturnsAsync(senderProfile);
        serviceMock
            .Setup(s => s.GetPlayerProfile(receiverId))
            .ReturnsAsync(receiverProfile);

        var result = await serviceMock.Object.StartFriendBattle(battleId, senderId, receiverId);

        Assert.NotNull(result);
        Assert.True(result!.IsMatched);
        Assert.Equal(senderId, result.Player!.UserId);
        Assert.Equal(receiverId, result.Opponent!.UserId);
        Assert.Equal(senderProfile, result.PlayerProfile);
        Assert.Equal(receiverProfile, result.OpponentProfile);
    }

    [Fact]
    public async Task StartFriendBattle_SenderProfileMissing_ReturnsNull()
    {
        int battleId = 1, senderId = 10, receiverId = 20;

        var serviceMock = new Mock<BattleMatchmakingService>(
            _matchmakingQueueRepoMock.Object,
            _userRepoMock.Object,
            _battleStatusRepoMock.Object,
            _battleResultRepoMock.Object
        );

        serviceMock
            .Setup(s => s.GetPlayerProfile(senderId))
            .ReturnsAsync((PlayerProfileDTO?)null);
        serviceMock
            .Setup(s => s.GetPlayerProfile(receiverId))
            .ReturnsAsync(new PlayerProfileDTO { UserId = receiverId });

        var result = await serviceMock.Object.StartFriendBattle(battleId, senderId, receiverId);

        Assert.Null(result);
    }

    [Fact]
    public async Task StartFriendBattle_ReceiverProfileMissing_ReturnsNull()
    {
        int battleId = 1, senderId = 10, receiverId = 20;

        var serviceMock = new Mock<BattleMatchmakingService>(
            _matchmakingQueueRepoMock.Object,
            _userRepoMock.Object,
            _battleStatusRepoMock.Object,
            _battleResultRepoMock.Object
        );

        serviceMock
            .Setup(s => s.GetPlayerProfile(senderId))
            .ReturnsAsync(new PlayerProfileDTO { UserId = senderId });
        serviceMock
            .Setup(s => s.GetPlayerProfile(receiverId))
            .ReturnsAsync((PlayerProfileDTO?)null);

        var result = await serviceMock.Object.StartFriendBattle(battleId, senderId, receiverId);

        Assert.Null(result);
    }

    [Fact]
    public async Task StartFriendBattle_BothProfilesMissing_ReturnsNull()
    {
        int battleId = 1, senderId = 10, receiverId = 20;

        var serviceMock = new Mock<BattleMatchmakingService>(
            _matchmakingQueueRepoMock.Object,
            _userRepoMock.Object,
            _battleStatusRepoMock.Object,
            _battleResultRepoMock.Object
        );

        serviceMock
            .Setup(s => s.GetPlayerProfile(It.IsAny<int>()))
            .ReturnsAsync((PlayerProfileDTO?)null);

        var result = await serviceMock.Object.StartFriendBattle(battleId, senderId, receiverId);

        Assert.Null(result);
    }
}
