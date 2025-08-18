using AutoMapper;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class BattleManagementServiceTests
    {
        private readonly Mock<ISqlQueryRepository> _mockSqlQueryRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly BattleManagementService _service;

        public BattleManagementServiceTests()
        {
            _mockSqlQueryRepository = new Mock<ISqlQueryRepository>();
            _mockMapper = new Mock<IMapper>();
            _service = new BattleManagementService(_mockSqlQueryRepository.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetBattleList_ShouldReturnMappedData_WhenRepositoryReturnsData()
        {
            // Arrange
            var repoData = new List<BattleManagementData>
            {
                new() { Id = 1, BattleName = "Battle 1", TotalXp = 100 },
                new() { Id = 2, BattleName = "Battle 2", TotalXp = 200 }
            };

            var mappedData = new List<BattleManagementData>
            {
                new() { Id = 1, BattleName = "Battle 1", TotalXp = 100 },
                new() { Id = 2, BattleName = "Battle 2", TotalXp = 200 }
            };

            _mockSqlQueryRepository
                .Setup(r => r.SqlQueryListAsync<BattleManagementData>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(repoData);

            _mockMapper
                .Setup(m => m.Map<List<BattleManagementData>>(repoData))
                .Returns(mappedData);

            // Act
            var result = await _service.GetBattleList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Battle 1", result[0].BattleName);

            _mockSqlQueryRepository.Verify(r =>
                r.SqlQueryListAsync<BattleManagementData>(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);

            _mockMapper.Verify(m => m.Map<List<BattleManagementData>>(repoData), Times.Once);
        }

        [Fact]
        public async Task GetBattleList_ShouldReturnEmptyList_WhenRepositoryReturnsEmpty()
        {
            // Arrange
            var repoData = new List<BattleManagementData>();
            var mappedData = new List<BattleManagementData>();

            _mockSqlQueryRepository
                .Setup(r => r.SqlQueryListAsync<BattleManagementData>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(repoData);

            _mockMapper
                .Setup(m => m.Map<List<BattleManagementData>>(repoData))
                .Returns(mappedData);

            // Act
            var result = await _service.GetBattleList();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetBattleList_ShouldThrowException_WhenRepositoryThrowsException()
        {
            // Arrange
            _mockSqlQueryRepository
                .Setup(r => r.SqlQueryListAsync<BattleManagementData>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.GetBattleList());
        }
    }
}
