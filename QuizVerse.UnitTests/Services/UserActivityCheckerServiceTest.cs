using System.Linq.Expressions;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class UserActivityCheckerServiceTest
{
    private readonly Mock<IGenericRepository<BattleStatus>> _mockBattleStatusRepository = new();
    private readonly Mock<IGenericRepository<QuizPlayStatus>> _mockQuizPlayStatusRepository = new();
    private readonly UserActivityCheckerService _service;

    public UserActivityCheckerServiceTest()
    {
        _service = new UserActivityCheckerService(
            _mockBattleStatusRepository.Object,
            _mockQuizPlayStatusRepository.Object
        );
    }

    [Fact]
    public async Task IsUserBusy_ShouldReturnTrue_WhenUserIsInBattle()
    {
        int userId = 1;
        _mockBattleStatusRepository
            .Setup(r => r.Exists(It.IsAny<Expression<Func<BattleStatus, bool>>>()))
            .ReturnsAsync(true);

        bool result = await _service.IsUserBusy(userId);

        Assert.True(result);
        _mockBattleStatusRepository.Verify(r => r.Exists(It.IsAny<Expression<Func<BattleStatus, bool>>>()), Times.Once);
        _mockQuizPlayStatusRepository.Verify(r => r.Exists(It.IsAny<Expression<Func<QuizPlayStatus, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task IsUserBusy_ShouldReturnTrue_WhenUserIsInQuizButNotInBattle()
    {
        int userId = 2;

        _mockBattleStatusRepository
            .Setup(r => r.Exists(It.IsAny<Expression<Func<BattleStatus, bool>>>()))
            .ReturnsAsync(false);

        _mockQuizPlayStatusRepository
            .Setup(r => r.Exists(It.IsAny<Expression<Func<QuizPlayStatus, bool>>>()))
            .ReturnsAsync(true);

        bool result = await _service.IsUserBusy(userId);

        Assert.True(result);
        _mockBattleStatusRepository.Verify(r => r.Exists(It.IsAny<Expression<Func<BattleStatus, bool>>>()), Times.Once);
        _mockQuizPlayStatusRepository.Verify(r => r.Exists(It.IsAny<Expression<Func<QuizPlayStatus, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task IsUserBusy_ShouldReturnFalse_WhenUserIsNotInBattleOrQuiz()
    {
        int userId = 3;

        _mockBattleStatusRepository
            .Setup(r => r.Exists(It.IsAny<Expression<Func<BattleStatus, bool>>>()))
            .ReturnsAsync(false);

        _mockQuizPlayStatusRepository
            .Setup(r => r.Exists(It.IsAny<Expression<Func<QuizPlayStatus, bool>>>()))
            .ReturnsAsync(false);

        bool result = await _service.IsUserBusy(userId);

        Assert.False(result);
        _mockBattleStatusRepository.Verify(r => r.Exists(It.IsAny<Expression<Func<BattleStatus, bool>>>()), Times.Once);
        _mockQuizPlayStatusRepository.Verify(r => r.Exists(It.IsAny<Expression<Func<QuizPlayStatus, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task IsUserBusy_ShouldOnlyCheckQuiz_WhenBattleIsFalse()
    {
        int userId = 4;

        _mockBattleStatusRepository
            .Setup(r => r.Exists(It.IsAny<Expression<Func<BattleStatus, bool>>>()))
            .ReturnsAsync(false);

        _mockQuizPlayStatusRepository
            .Setup(r => r.Exists(It.IsAny<Expression<Func<QuizPlayStatus, bool>>>()))
            .ReturnsAsync(false);

        await _service.IsUserBusy(userId);

        _mockBattleStatusRepository.Verify(r => r.Exists(It.IsAny<Expression<Func<BattleStatus, bool>>>()), Times.Once);
        _mockQuizPlayStatusRepository.Verify(r => r.Exists(It.IsAny<Expression<Func<QuizPlayStatus, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task IsUserBusy_ShouldNotCheckQuiz_WhenBattleIsTrue()
    {
        int userId = 5;

        _mockBattleStatusRepository
            .Setup(r => r.Exists(It.IsAny<Expression<Func<BattleStatus, bool>>>()))
            .ReturnsAsync(true);

        await _service.IsUserBusy(userId);

        _mockBattleStatusRepository.Verify(r => r.Exists(It.IsAny<Expression<Func<BattleStatus, bool>>>()), Times.Once);
        _mockQuizPlayStatusRepository.Verify(r => r.Exists(It.IsAny<Expression<Func<QuizPlayStatus, bool>>>()), Times.Never);
    }
}
