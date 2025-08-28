using AutoMapper;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class QuestionDifficultyServiceTest
    {
        private readonly Mock<IGenericRepository<QuestionDifficulty>> _mockRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly QuestionDifficultyService _service;

        public QuestionDifficultyServiceTest()
        {
            _mockRepo = new Mock<IGenericRepository<QuestionDifficulty>>();
            _mockMapper = new Mock<IMapper>();
            _service = new QuestionDifficultyService(_mockRepo.Object, _mockMapper.Object);
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
    }
}
