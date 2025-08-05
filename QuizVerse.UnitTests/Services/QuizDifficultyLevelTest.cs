using AutoMapper;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class QuizDifficultyLevelTest
{
    private readonly Mock<IGenericRepository<QuizDifficulty>> _mockRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly QuizDifficultyLevelService _service;

    public QuizDifficultyLevelTest()
    {
        _mockRepo = new Mock<IGenericRepository<QuizDifficulty>>();
        _mockMapper = new Mock<IMapper>();
        _service = new QuizDifficultyLevelService(_mockRepo.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetQuizDifficultyList_ShouldReturnMappedList_WhenDataExists()
    {
        var quizDifficulties = new List<QuizDifficulty>
            {
                new() { Id = 1, Name = "Easy", Description = "Easy level", IsDeleted = false },
                new() { Id = 2, Name = "Medium", Description = "Medium level", IsDeleted = false },
                new() { Id = 3, Name = "Hard", Description = "Hard level", IsDeleted = true }
            };

        var expectedMappedList = new List<QuizDifficultyResponse>
            {
                new() { Id = 1, Name = "Easy", Description = "Easy level" },
                new() { Id = 2, Name = "Medium", Description = "Medium level" }
            };

        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(quizDifficulties);

        _mockMapper.Setup(m =>
            m.Map<List<QuizDifficultyResponse>>(It.Is<List<QuizDifficulty>>(src =>
                src.Count == 2 && src.TrueForAll(d => !d.IsDeleted))))
            .Returns(expectedMappedList);

        var result = await _service.GetQuizDifficultyList();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Easy", result[0].Name);
        Assert.Equal("Medium", result[1].Name);
    }

    [Fact]
    public async Task GetQuizDifficultyList_ShouldReturnEmptyList_WhenNoDataExists()
    {
        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<QuizDifficulty>());
        _mockMapper.Setup(m => m.Map<List<QuizDifficultyResponse>>(It.IsAny<List<QuizDifficulty>>()))
                   .Returns([]);

        var result = await _service.GetQuizDifficultyList();

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}

