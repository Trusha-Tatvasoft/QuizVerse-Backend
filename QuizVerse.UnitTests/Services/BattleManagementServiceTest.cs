using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using Npgsql;
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
        private readonly BattleManagementService _service;

        public BattleManagementServiceTests()
        {
            _mockHttpContext.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());

            _service = new BattleManagementService(
                _mockSqlRepo.Object,
                _mockMapper.Object,
                _mockHttpContext.Object,
                _mockBattleRepo.Object);
        }

        #region GetBattleList

        [Fact]
        public async Task GetBattleList_ShouldReturnMappedData()
        {
            var repoData = new List<BattleManagementData> { new() { Id = 1, BattleName = "B1" } };
            var mapped = new List<BattleManagementData> { new() { Id = 1, BattleName = "B1" } };

            _mockSqlRepo
                .Setup(r => r.SqlQueryListAsync<BattleManagementData>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(repoData);

            _mockMapper
                .Setup(m => m.Map<List<BattleManagementData>>(repoData))
                .Returns(mapped);

            var result = await _service.GetBattleList();

            Assert.Single(result);
            Assert.Equal("B1", result[0].BattleName);
        }

        [Fact]
        public async Task GetBattleList_ShouldPropagateException()
        {
            _mockSqlRepo
                .Setup(r => r.SqlQueryListAsync<BattleManagementData>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ThrowsAsync(new Exception("DB fail"));

            await Assert.ThrowsAsync<Exception>(() => _service.GetBattleList());
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
        public async Task CreateUpdateBattle_Update_NoUser()
        {
            var req = new SaveBattleRequestDTO
            {
                Id = 7,
                Name = "Upd",
                Description = "D",
                DifficultyLevelId = 2,
                CategoryId = 3,
                Status = (int)BattleCreationStatus.Completed,
                BattleType = (int)BattleType.Permanent,
                TotalTime = 60,
                TotalQuestion = 20,
                TotalXp = 200,
                Questions = null,
                QuestionsDifficulty = null,
                QuizTypes = (int)QuizType.Battle
            };

            object[]? captured = null;

            _mockSqlRepo
                .Setup(r => r.SqlQuerySingleAsync<CreateUpdateResponseDto>(It.IsAny<string>(), It.IsAny<object[]>()))
                .Callback<string, object[]>((_, p) => captured = p)
                .ReturnsAsync(new CreateUpdateResponseDto { Success = true, Message = "updated" });

            var result = await _service.CreateUpdateBattle(req);

            Assert.Equal("updated", result.Message);
            Assert.NotNull(captured);
            var cb = ((NpgsqlParameter)captured!.Single(x => ((NpgsqlParameter)x).ParameterName == "p_created_by")).Value;
            Assert.Equal(DBNull.Value, cb);
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
        public async Task DeleteBattle_ShouldSoftDelete()
        {
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
            _mockBattleRepo
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<BattleList, bool>>>(), null))
                .ReturnsAsync((BattleList)null!);

            await Assert.ThrowsAsync<AppException>(() => _service.DeleteBattle(123));
        }

        #endregion
    }
}
