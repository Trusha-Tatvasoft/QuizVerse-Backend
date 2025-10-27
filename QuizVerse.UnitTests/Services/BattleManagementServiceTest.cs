using System.Linq.Expressions;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class BattleManagementServiceTests
    {
        private readonly Mock<ISqlQueryRepository> _mockSqlRepo = new();
        private readonly Mock<IMapper> _mockMapper = new();
        private readonly Mock<IHttpContextAccessor> _mockHttpContext = new();
        private readonly Mock<IGenericRepository<BattleList>> _mockBattleRepo = new();
        private readonly Mock<IGenericRepository<Domain.Entities.BattleStatus>> _mockBattleStatusRepo = new();
        private readonly BattleManagementService _service;

        public BattleManagementServiceTests()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.UserData, "1")], "mock"));

            _mockHttpContext.Setup(x => x.HttpContext).Returns(httpContext);

            _service = new BattleManagementService(
                _mockSqlRepo.Object,
                _mockMapper.Object,
                _mockHttpContext.Object,
                _mockBattleRepo.Object,
                _mockBattleStatusRepo.Object
            );
        }

        #region GetBattleList

        [Fact]
        public async Task GetBattleList_ShouldReturnMappedData()
        {
            // Arrange
            // Raw result returned from SQL — JSON string for battles
            var rawRepoResult = new BattleManagementRawResult
            {
                Battles = "[{\"Id\":1,\"BattleName\":\"B1\"}]", // ✅ JSON string
                HasMore = false
            };

            var mappedResponse = new BattleManagementDataResponseDto
            {
                Battles = new List<BattleManagementData>
                {
                    new() { Id = 1, BattleName = "B1" }
                },
                HasMore = false
            };

            _mockSqlRepo
                .Setup(r => r.SqlQuerySingleAsync<BattleManagementRawResult>(
                    It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(rawRepoResult);

            _mockMapper
                .Setup(m => m.Map<BattleManagementDataResponseDto>(rawRepoResult))
                .Returns(mappedResponse);

            _mockMapper
                .Setup(m => m.Map<BattleManagementData>(It.IsAny<BattleManagementData>()))
                .Returns<BattleManagementData>(b => b);

            // Act
            var result = await _service.GetBattleList(1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Battles);
            Assert.Equal("B1", result.Battles[0].BattleName);
            Assert.False(result.HasMore);
        }

        [Fact]
        public async Task GetBattleList_ShouldPropagateException()
        {
            _mockSqlRepo
                .Setup(r => r.SqlQuerySingleAsync<BattleManagementRawResult>(
                    It.IsAny<string>(), It.IsAny<object[]>()))
                .ThrowsAsync(new Exception("DB fail"));

            await Assert.ThrowsAsync<Exception>(() => _service.GetBattleList(1));
        }

        #endregion

        #region CreateUpdateBattle

        [Fact]
        public async Task CreateUpdateBattle_Create_WithUserId()
        {
            var req = new SaveBattleRequestDTO
            {
                Name = "Test",
                Description = "Desc",
                DifficultyLevelId = 1,
                CategoryId = 2,
                Status = (int)BattleCreationStatus.Active,
                BattleType = (int)BattleType.TimeLimited,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                TotalTime = 30,
                TotalQuestion = 10,
                TotalXp = 100,
                Questions = new() { new QuestionsListRequestDto { CategoryId = 1, QueDifficultyId = 1, QueText = "Q1", QueTypeId = 1 } },
                QuestionsDifficulty = new() { new BattleQuestionDifficultyDTO { QueDifficultyId = 1, NoOfQues = 5, TimePerQuestion = 30 } },
                QuizTypes = (int)QuizType.Battle
            };

            object[]? captured = null;

            _mockSqlRepo
                .Setup(r => r.SqlQuerySingleAsync<CreateUpdateResponseDto>(It.IsAny<string>(), It.IsAny<object[]>()))
                .Callback<string, object[]>((_, p) => captured = p)
                .ReturnsAsync(new CreateUpdateResponseDto { Success = true, Message = "ok" });

            var result = await _service.CreateUpdateBattle(req);

            Assert.True(result.Success);
            Assert.NotNull(captured);
        }

        [Fact]
        public async Task CreateUpdateBattle_ShouldThrow_OnNullRequest()
        {
            await Assert.ThrowsAsync<AppException>(() => _service.CreateUpdateBattle(null!));
        }

        [Fact]
        public async Task CreateUpdateBattle_ShouldThrow_WhenRepositoryReturnsNull()
        {
            var req = new SaveBattleRequestDTO { Name = "X", Description = "D", DifficultyLevelId = 1, CategoryId = 1, Status = 1, TotalTime = 1, TotalQuestion = 1, TotalXp = 1 };
            _mockSqlRepo
                .Setup(r => r.SqlQuerySingleAsync<CreateUpdateResponseDto>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync((CreateUpdateResponseDto)null!);

            var ex = await Assert.ThrowsAsync<AppException>(() => _service.CreateUpdateBattle(req));
            Assert.Equal(Constants.CREATE_OR_UPDATE_BATTLE_FAILED, ex.Message);
        }

        [Fact]
        public async Task CreateUpdateBattle_ShouldThrow_WhenRepositoryFailure()
        {
            var req = new SaveBattleRequestDTO { Name = "X", Description = "D", DifficultyLevelId = 1, CategoryId = 1, Status = 1, TotalTime = 1, TotalQuestion = 1, TotalXp = 1 };
            _mockSqlRepo
                .Setup(r => r.SqlQuerySingleAsync<CreateUpdateResponseDto>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(new CreateUpdateResponseDto { Success = false, Message = "fail" });

            var ex = await Assert.ThrowsAsync<AppException>(() => _service.CreateUpdateBattle(req));
            Assert.Equal("fail", ex.Message);
        }

        #endregion

        #region GetBattleById

        [Fact]
        public async Task GetBattleById_ShouldReturnMapped()
        {
            _mockBattleRepo.Setup(r => r.Exists(It.IsAny<Expression<Func<BattleList, bool>>>())).ReturnsAsync(true);
            var dbDto = new BattleResponseDto { Id = 1, Name = "DB" };
            _mockSqlRepo
                .Setup(r => r.SqlQuerySingleAsync<BattleResponseDto>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(dbDto);
            _mockMapper.Setup(m => m.Map<BattleResponseDto>(dbDto)).Returns(dbDto);

            var result = await _service.GetBattleById(1);

            Assert.Equal("DB", result.Name);
        }

        [Fact]
        public async Task GetBattleById_ShouldThrow_WhenNotFound()
        {
            _mockBattleRepo.Setup(r => r.Exists(It.IsAny<Expression<Func<BattleList, bool>>>())).ReturnsAsync(false);

            await Assert.ThrowsAsync<AppException>(() => _service.GetBattleById(99));
        }

        #endregion

        #region DeleteBattle
        [Fact]
        public async Task DeleteBattle_ShouldThrow_WhenBattleIsBeingPlayed()
        {
            _mockBattleStatusRepo
                .Setup(r => r.Exists(It.IsAny<Expression<Func<Domain.Entities.BattleStatus, bool>>>()))
                .ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<AppException>(() => _service.DeleteBattle(10));
            Assert.Equal(Constants.CAN_NOT_DELETE_BATTLE, ex.Message);
        }

        [Fact]
        public async Task DeleteBattle_ShouldSoftDelete()
        {
            _mockBattleStatusRepo
                .Setup(r => r.Exists(It.IsAny<Expression<Func<Domain.Entities.BattleStatus, bool>>>()))
                .ReturnsAsync(false);

            var b = new BattleList { Id = 5, IsDeleted = false };
            _mockBattleRepo
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ReturnsAsync(b);
            _mockBattleRepo.Setup(r => r.UpdateAsync(b)).Returns(Task.CompletedTask);

            var result = await _service.DeleteBattle(5);

            Assert.True(b.IsDeleted);
            Assert.NotNull(b.ModifiedDate);
            Assert.Equal(Constants.DELETE_SUCCESS, result);
        }

        [Fact]
        public async Task DeleteBattle_ShouldThrow_WhenNotFound()
        {
            _mockBattleStatusRepo
                .Setup(r => r.Exists(It.IsAny<Expression<Func<Domain.Entities.BattleStatus, bool>>>()))
                .ReturnsAsync(false);
                
            _mockBattleRepo
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ReturnsAsync((BattleList)null!);

            await Assert.ThrowsAsync<AppException>(() => _service.DeleteBattle(123));
        }

        #endregion
    }
}
