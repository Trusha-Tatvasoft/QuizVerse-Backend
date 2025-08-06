using AutoMapper;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class QuizTagsServiceTest
{
    private readonly Mock<IGenericRepository<QuizTag>> _mockRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly QuizTagsService _service;

    public QuizTagsServiceTest()
    {
        _mockRepo = new Mock<IGenericRepository<QuizTag>>();
        _mockMapper = new Mock<IMapper>();
        _service = new QuizTagsService(_mockRepo.Object, _mockMapper.Object);
    }

    [Fact]
    public void GetAllQuizTags_ShouldReturnMappedDropdownList()
    {
        // Arrange
        var quizTags = new List<QuizTag>
            {
                new QuizTag { Id = 1, TagName = "Science" },
                new QuizTag { Id = 2, TagName = "NodeJs" }
            }.AsQueryable();

        var expectedDtos = new List<CommonListDropDownDto>
            {
                new CommonListDropDownDto { Id = 1, Name = "Science" },
                new CommonListDropDownDto { Id = 2, Name = "NodeJs" }
            };

        _mockRepo.Setup(r => r.GetQueryableInclude()).Returns(quizTags);

        _mockMapper
            .Setup(m => m.ProjectTo<CommonListDropDownDto>(
                It.IsAny<IQueryable<QuizTag>>(),
                It.IsAny<object>()))
            .Returns(expectedDtos.AsQueryable());


        // Act
        var result = _service.GetAllQuizTags();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Science", result[0].Name);
        Assert.Equal("NodeJs", result[1].Name);
    }

    [Fact]
    public void GetAllQuizTags_WhenNoData_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyTags = new List<QuizTag>().AsQueryable();
        var emptyDtos = new List<CommonListDropDownDto>().AsQueryable();

        _mockRepo.Setup(r => r.GetQueryableInclude()).Returns(emptyTags);

        _mockMapper.Setup(m => m.ProjectTo<CommonListDropDownDto>(It.IsAny<IQueryable<QuizTag>>(), null))
            .Returns(emptyDtos);

        // Act
        var result = _service.GetAllQuizTags();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}