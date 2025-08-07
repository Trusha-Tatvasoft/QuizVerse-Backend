using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using Xunit;
using System.Linq.Expressions;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Common;

namespace QuizVerse.UnitTests.Services;

public class QuizDifficultyLevelTest
{
    private readonly Mock<IGenericRepository<QuizDifficulty>> _mockRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly QuizDifficultyLevelService _service;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

    public QuizDifficultyLevelTest()
    {
        _mockRepo = new Mock<IGenericRepository<QuizDifficulty>>();
        _mockMapper = new Mock<IMapper>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        // Mock authenticated user with ClaimTypes.UserData
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.UserData, "1")], "mock"));

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        _service = new QuizDifficultyLevelService(_mockRepo.Object, _mockMapper.Object, _mockHttpContextAccessor.Object);
    }

    #region Get Difficulty List
    [Fact]
    public async Task GetQuizDifficultyList_ShouldReturnMappedList_WhenDataExists()
    {
        var quizDifficulties = new List<QuizDifficulty>
        {
            new() { Id = 1, Name = "Easy", Description = "Easy level", IsDeleted = false },
            new() { Id = 2, Name = "Medium", Description = "Medium level", IsDeleted = false },
            new() { Id = 3, Name = "Hard", Description = "Hard level", IsDeleted = true }
        };

        var expectedMappedList = new List<QuizDifficultyDTO>
        {
            new() { Id = 1, Name = "Easy", Description = "Easy level" },
            new() { Id = 2, Name = "Medium", Description = "Medium level" }
        };

        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(quizDifficulties);

        _mockMapper.Setup(m =>
            m.Map<List<QuizDifficultyDTO>>(It.Is<List<QuizDifficulty>>(src =>
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
        _mockMapper.Setup(m => m.Map<List<QuizDifficultyDTO>>(It.IsAny<List<QuizDifficulty>>())).Returns([]);

        var result = await _service.GetQuizDifficultyList();

        Assert.NotNull(result);
        Assert.Empty(result);
    }
    #endregion

    #region Get By Id 

    [Fact]
    public async Task GetDifficultyLevelById_ShouldReturnMappedDTO_WhenRecordExists()
    {
        var difficulty = new QuizDifficulty { Id = 1, Name = "Easy", Description = "Easy", IsDeleted = false };
        var dto = new QuizDifficultyDTO { Id = 1, Name = "Easy", Description = "Easy" };

        _mockRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuizDifficulty, bool>>>(), null))
                 .ReturnsAsync(difficulty);

        _mockMapper.Setup(m => m.Map<QuizDifficultyDTO>(difficulty)).Returns(dto);

        var result = await _service.GetDifficultyLevelById(1);

        Assert.NotNull(result);
        Assert.Equal(dto.Id, result.Id);
        Assert.Equal(dto.Name, result.Name);
    }

    #endregion

    #region Create Difficulty Level
    [Fact]
    public async Task CreateDifficultyLevel_ShouldAddNewDifficulty_WhenNameIsValid()
    {
        var requestDto = new QuizDifficultyRequestDto
        {
            Name = "UniqueName",
            Description = "Test Description"
        };

        var entity = new QuizDifficulty
        {
            Name = requestDto.Name,
            Description = requestDto.Description
        };

        _mockRepo.Setup(r => r.Exists(It.IsAny<Expression<Func<QuizDifficulty, bool>>>()))
                 .ReturnsAsync(false);

        _mockMapper.Setup(m => m.Map<QuizDifficulty>(requestDto)).Returns(entity);

        _mockRepo.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);

        var result = await _service.CreateDifficultyLevel(requestDto);

        Assert.Equal(Constants.CREATE_SUCCESS, result);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<QuizDifficulty>()), Times.Once);
    }

    [Fact]
    public async Task CreateDifficultyLevel_ShouldThrowException_WhenNameAlreadyExists()
    {
        var requestDto = new QuizDifficultyRequestDto
        {
            Name = "DuplicateName",
            Description = "Test Description"
        };

        _mockRepo.Setup(r => r.Exists(It.IsAny<Expression<Func<QuizDifficulty, bool>>>()))
                 .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<AppException>(() => _service.CreateDifficultyLevel(requestDto));

        Assert.Equal(Constants.DUPLICATE_DIFFICULTY_LEVEL_NAME, exception.Message);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<QuizDifficulty>()), Times.Never);
    }
    #endregion

    #region Name Valid
    [Fact]
    public async Task NameValid_ShouldThrowException_WhenNameExistsAndNotDeleted()
    {
        // Arrange
        string testName = "Easy";

        _mockRepo.Setup(r => r.Exists(It.IsAny<Expression<Func<QuizDifficulty, bool>>>()))
                 .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() => _service.NameValid(testName));
        Assert.Equal(Constants.DUPLICATE_DIFFICULTY_LEVEL_NAME, exception.Message);
    }

    [Fact]
    public async Task NameValid_ShouldReturnTrue_WhenNameDoesNotExist()
    {
        // Arrange
        string testName = "Advanced";

        _mockRepo.Setup(r => r.Exists(It.IsAny<Expression<Func<QuizDifficulty, bool>>>()))
                 .ReturnsAsync(false);

        // Act
        var result = await _service.NameValid(testName);

        // Assert
        Assert.True(result);
    }
    #endregion
}

