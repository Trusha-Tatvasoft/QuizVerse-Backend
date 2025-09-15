using System.Linq.Expressions;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class QuestionDifficultyServiceTest
    {
        private readonly Mock<IGenericRepository<QuestionDifficulty>> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IDropDownDataService> _mockDropDown;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly QuestionDifficultyService _service;

        public QuestionDifficultyServiceTest()
        {
            _mockRepo = new Mock<IGenericRepository<QuestionDifficulty>>();
            _mockMapper = new Mock<IMapper>();
            _mockDropDown = new Mock<IDropDownDataService>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            // Fake user with claims
            var testUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.UserData, "123")
            }, "mock"));

            var context = new DefaultHttpContext { User = testUser };
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(context);

            _service = new QuestionDifficultyService(
                _mockRepo.Object,
                _mockMapper.Object,
                _mockHttpContextAccessor.Object,
                _mockDropDown.Object
            );
        }

        [Fact]
        public async Task GetBattleQuestionDifficultyData_ShouldReturnMappedData()
        {
            // Arrange
            var repoData = new List<QuestionDifficulty>
            {
                new() { Id = 1, Name = "Easy", XpGained = 10, IsDeleted = false },
                new() { Id = 2, Name = "Medium", XpGained = 20, IsDeleted = true },
                new() { Id = 3, Name = "Hard", XpGained = 30, IsDeleted = false }
            };

            var expectedMapped = new List<QuestionDifficultyXPData>
            {
                new() { QuestionDifficultyId = 1, QuestionDifficultyName = "Easy", XpGained = 10 },
                new() { QuestionDifficultyId = 3, QuestionDifficultyName = "Hard", XpGained = 30 }
            };

            _mockRepo.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(repoData);

            _mockMapper.Setup(m => m.Map<List<QuestionDifficultyXPData>>(It.IsAny<IEnumerable<QuestionDifficulty>>()))
                       .Returns((IEnumerable<QuestionDifficulty> source) =>
                           source.Select(q => new QuestionDifficultyXPData
                           {
                               QuestionDifficultyId = q.Id,
                               QuestionDifficultyName = q.Name,
                               XpGained = q.XpGained
                           }).ToList()
                       );

            // Act
            var result = await _service.GetBattleQuestionDifficultyData();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // only 2 not deleted
            Assert.Contains(result, x => x.QuestionDifficultyName == "Easy");
            Assert.Contains(result, x => x.QuestionDifficultyName == "Hard");

            _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
            _mockMapper.Verify(m => m.Map<List<QuestionDifficultyXPData>>(It.IsAny<IEnumerable<QuestionDifficulty>>()), Times.Once);
        }

        [Fact]
        public void GetQuestionDifficulties_ShouldReturnMappedDTOs_WithCorrectTotalQuestions()
        {
            // Arrange
            var q1 = new QuestionDifficulty
            {
                Id = 1,
                Name = "Easy",
                IsDeleted = false,
                BaseQuestions = new List<BaseQuestion>
                {
                    new() { Id = 101, IsDeleted = false },
                    new() { Id = 102, IsDeleted = true } // should not count
                }
            };

            var q2 = new QuestionDifficulty
            {
                Id = 2,
                Name = "Hard",
                IsDeleted = false,
                BaseQuestions = new List<BaseQuestion>
                {
                    new() { Id = 201, IsDeleted = false },
                    new() { Id = 202, IsDeleted = false }
                }
            };

            var entities = new List<QuestionDifficulty> { q1, q2 }.AsQueryable();

            _mockRepo.Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<QuestionDifficulty, object>>[]>()))
                     .Returns(entities);

            _mockMapper.Setup(m => m.Map<List<QuestionDifficultyResponseDTO>>(It.IsAny<IQueryable<QuestionDifficulty>>()))
                       .Returns((IQueryable<QuestionDifficulty> src) =>
                           src.Select(q => new QuestionDifficultyResponseDTO
                           {
                               Id = q.Id,
                               Name = q.Name,
                               Description = q.Description,
                               XpGained = q.XpGained
                           }).ToList()
                       );

            // Act
            var result = _service.GetQuestionDifficulties();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            var easy = result.First(r => r.Id == 1);
            var hard = result.First(r => r.Id == 2);

            Assert.Equal(1, easy.TotalQuestions); // only 1 not deleted
            Assert.Equal(2, hard.TotalQuestions); // 2 not deleted
        }

        [Fact]
        public void GetQuestionDifficulties_ShouldReturnEmpty_WhenAllDeleted()
        {
            // Arrange
            var q1 = new QuestionDifficulty { Id = 1, Name = "Easy", IsDeleted = true };
            var entities = new List<QuestionDifficulty> { q1 }.AsQueryable();

            _mockRepo.Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<QuestionDifficulty, object>>[]>()))
                     .Returns(entities);

            _mockMapper.Setup(m => m.Map<List<QuestionDifficultyResponseDTO>>(It.IsAny<IQueryable<QuestionDifficulty>>()))
                       .Returns(new List<QuestionDifficultyResponseDTO>());

            // Act
            var result = _service.GetQuestionDifficulties();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task AddOrEditQuestionDifficulty_ShouldAddNew_WhenNoConflict()
        {
            // Arrange
            var request = new QuestionDifficultyRequestDTO
            {
                Id = 0,
                Name = "Easy",
                Description = "Beginner",
                XpGainedPerQuestion = 10
            };

            _mockRepo.Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<QuestionDifficulty, bool>>>(),
                    It.IsAny<Func<IQueryable<QuestionDifficulty>, IQueryable<QuestionDifficulty>>>()))
                .ReturnsAsync((QuestionDifficulty?)null);

            // Act
            var result = await _service.AddOrEditQuestionDifficulty(request);

            // Assert
            Assert.Equal("Question Difficulty added successfully.", result);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<QuestionDifficulty>()), Times.Once);
        }

        [Fact]
        public async Task AddOrEditQuestionDifficulty_ShouldThrow_WhenDuplicateActiveExists()
        {
            // Arrange
            var request = new QuestionDifficultyRequestDTO { Id = 0, Name = "Easy" };
            _mockRepo.Setup(r => r.GetAsync(
          It.IsAny<Expression<Func<QuestionDifficulty, bool>>>(),
          It.IsAny<Func<IQueryable<QuestionDifficulty>, IQueryable<QuestionDifficulty>>>()))
            .ReturnsAsync(new QuestionDifficulty { Id = 1, Name = "Easy", IsDeleted = false });

            // Act & Assert
            await Assert.ThrowsAsync<AppException>(() => _service.AddOrEditQuestionDifficulty(request));
        }

        [Fact]
        public async Task AddOrEditQuestionDifficulty_ShouldUpdate_WhenNameUnchanged()
        {
            // Arrange
            var existing = new QuestionDifficulty { Id = 1, Name = "Easy", XpGained = 10, IsDeleted = false };
            var request = new QuestionDifficultyRequestDTO { Id = 1, Name = "Easy", Description = "Updated desc", XpGainedPerQuestion = 50 };

            _mockRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuestionDifficulty, bool>>>(), null))
                     .ReturnsAsync(existing);

            // no XP conflict
            _mockRepo.Setup(r => r.GetAsync(It.Is<Expression<Func<QuestionDifficulty, bool>>>(expr =>
                expr.Compile().Invoke(new QuestionDifficulty { Id = 2, XpGained = 50, IsDeleted = false })), null))
                     .ReturnsAsync((QuestionDifficulty?)null);

            // Act
            var result = await _service.AddOrEditQuestionDifficulty(request);

            // Assert
            Assert.Equal(Constants.QUESTION_DIFFICULTY_UPDATED, result);
            Assert.Equal("Updated desc", existing.Description);
            Assert.Equal(50, existing.XpGained);
            _mockRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task AddOrEditQuestionDifficulty_ShouldRevive_WhenDuplicateDeletedExists()
        {
            // Arrange
            var request = new QuestionDifficultyRequestDTO { Id = 0, Name = "Easy", Description = "Revived", XpGainedPerQuestion = 30 };
            var deletedEntity = new QuestionDifficulty { Id = 1, Name = "Easy", IsDeleted = true, XpGained = 15 };

            // No XP conflict for 30
            _mockRepo.Setup(r => r.GetAsync(It.Is<Expression<Func<QuestionDifficulty, bool>>>(expr =>
                expr.Compile().Invoke(new QuestionDifficulty { XpGained = 30, IsDeleted = false })), null))
                     .ReturnsAsync((QuestionDifficulty?)null);

            // Returns deleted entity on name check
            _mockRepo.Setup(r => r.GetAsync(It.Is<Expression<Func<QuestionDifficulty, bool>>>(expr =>
                expr.Compile().Invoke(deletedEntity)), null))
                     .ReturnsAsync(deletedEntity);

            // Act
            var result = await _service.AddOrEditQuestionDifficulty(request);

            // Assert
            Assert.Equal(Constants.QUESTION_DIFFICULTY_ADDED, result);
            Assert.False(deletedEntity.IsDeleted);
            Assert.Equal("Revived", deletedEntity.Description);
            Assert.Equal(30, deletedEntity.XpGained);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<QuestionDifficulty>()), Times.Once);
        }

        [Fact]
        public async Task AddOrEditQuestionDifficulty_ShouldThrow_WhenEditingNonExistent()
        {
            // Arrange
            var request = new QuestionDifficultyRequestDTO { Id = 99, Name = "Medium" };

            _mockRepo.Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<QuestionDifficulty, bool>>>(),
                    It.IsAny<Func<IQueryable<QuestionDifficulty>, IQueryable<QuestionDifficulty>>>()))
                .ReturnsAsync((QuestionDifficulty?)null);

            // Act & Assert
            await Assert.ThrowsAsync<AppException>(() => _service.AddOrEditQuestionDifficulty(request));
        }

        [Fact]
        public async Task AddOrEditQuestionDifficulty_ShouldThrow_WhenRenamingToExistingActive()
        {
            // Arrange
            var existing = new QuestionDifficulty { Id = 1, Name = "Easy", IsDeleted = false };
            var conflict = new QuestionDifficulty { Id = 2, Name = "Medium", IsDeleted = false };
            var request = new QuestionDifficultyRequestDTO { Id = 1, Name = "Medium" };

            _mockRepo.SetupSequence(r => r.GetAsync(It.IsAny<Expression<Func<QuestionDifficulty, bool>>>(), null))
                     .ReturnsAsync(existing)  // first call → load entity by Id
                     .ReturnsAsync(conflict); // second call → conflict check

            // Act & Assert
            await Assert.ThrowsAsync<AppException>(() => _service.AddOrEditQuestionDifficulty(request));
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<QuestionDifficulty>()), Times.Never);
        }

        [Fact]
        public async Task AddOrEditQuestionDifficulty_ShouldThrow_WhenRequestIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddOrEditQuestionDifficulty(null));
        }

        [Fact]
        public async Task DeleteQuestionDifficulty_ShouldMarkDeleted_WhenExists()
        {
            // Arrange
            var entity = new QuestionDifficulty { Id = 1, Name = "Hard", IsDeleted = false };

            _mockRepo.Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<QuestionDifficulty, bool>>>(),
                    It.IsAny<Func<IQueryable<QuestionDifficulty>, IQueryable<QuestionDifficulty>>>()))
                .ReturnsAsync(entity);

            // Act
            var result = await _service.DeleteQuestionDifficulty(1);

            // Assert
            Assert.Equal("Question Difficulty deleted successfully.", result);
            Assert.True(entity.IsDeleted);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<QuestionDifficulty>()), Times.Once);
        }

        [Fact]
        public async Task DeleteQuestionDifficulty_ShouldThrow_WhenNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<QuestionDifficulty, bool>>>(),
                    It.IsAny<Func<IQueryable<QuestionDifficulty>, IQueryable<QuestionDifficulty>>>()))
                .ReturnsAsync((QuestionDifficulty?)null);

            // Act & Assert
            await Assert.ThrowsAsync<AppException>(() => _service.DeleteQuestionDifficulty(99));
        }

        [Fact]
        public async Task IsQuestionDifficultyNameAvailable_WhenNameNotExists_ReturnsTrue()
        {
            // Arrange
            string name = "Easy";
            _mockRepo.Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionDifficulty, bool>>>()))
                     .ReturnsAsync(false);

            // Act
            var result = await _service.IsQuestionDifficultyNameAvailable(name);

            // Assert
            Assert.True(result);
            _mockRepo.Verify(r => r.Exists(It.IsAny<Expression<Func<QuestionDifficulty, bool>>>()), Times.Once);
        }

        [Fact]
        public async Task IsQuestionDifficultyNameAvailable_WhenNameExists_ThrowsAppException()
        {
            // Arrange
            string name = "Easy";
            _mockRepo.Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionDifficulty, bool>>>()))
                     .ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() =>
                _service.IsQuestionDifficultyNameAvailable(name));

            Assert.Equal(Constants.QUESTION_DIFFICULTY_DUPLICATE_NAME, ex.Message);
        }

        [Fact]
        public async Task IsQuestionDifficultyXPAvailable_WhenXPNotExists_ReturnsTrue()
        {
            // Arrange
            int xp = 50;
            _mockRepo.Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionDifficulty, bool>>>()))
                     .ReturnsAsync(false);

            // Act
            var result = await _service.IsQuestionDifficultyXPAvailable(xp);

            // Assert
            Assert.True(result);
            _mockRepo.Verify(r => r.Exists(It.IsAny<Expression<Func<QuestionDifficulty, bool>>>()), Times.Once);
        }

        [Fact]
        public async Task IsQuestionDifficultyXPAvailable_WhenXPExists_ThrowsAppException()
        {
            // Arrange
            int xp = 50;
            _mockRepo.Setup(r => r.Exists(It.IsAny<Expression<Func<QuestionDifficulty, bool>>>()))
                     .ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() =>
                _service.IsQuestionDifficultyXPAvailable(xp));

            Assert.Equal(Constants.QUESTION_DIFFICULTY_DUPLICATE_XP, ex.Message);
        }
    }
}
