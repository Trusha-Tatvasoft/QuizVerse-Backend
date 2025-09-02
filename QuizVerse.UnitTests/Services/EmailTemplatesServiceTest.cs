using AutoMapper;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class EmailTemplatesServiceTest
{
    private readonly Mock<IGenericRepository<EmailTemplete>> _emailTemplateRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly EmailTemplatesService _emailTemplatesService;

    public EmailTemplatesServiceTest()
    {
        _emailTemplateRepositoryMock = new Mock<IGenericRepository<EmailTemplete>>();
        _mapperMock = new Mock<IMapper>();
        _emailTemplatesService = new EmailTemplatesService(_emailTemplateRepositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public void GetAllEmailTemplates_DefaultSorting_ReturnsAllTemplates()
    {
        // Arrange
        var pageListRequest = new PageListRequest
        {
            SortColumn = null,
            SortDescending = false
        };

        var emailTemplates = new List<EmailTemplete>
        {
            new EmailTemplete { Id = 1, Title = "Template1", IsDeleted = false },
            new EmailTemplete { Id = 2, Title = "Template2", IsDeleted = false }
        }.AsQueryable();

        var mappedTemplates = new List<EmailTemplatesResponseDto>
        {
            new EmailTemplatesResponseDto { Id = 1, Title = "Template1" },
            new EmailTemplatesResponseDto { Id = 2, Title = "Template2" }
        };

        _emailTemplateRepositoryMock
            .Setup(repo => repo.GetQueryableInclude())
            .Returns(emailTemplates);

        _mapperMock
            .Setup(mapper => mapper.Map<List<EmailTemplatesResponseDto>>(It.IsAny<List<EmailTemplete>>()))
            .Returns(mappedTemplates);

        // Act
        var result = _emailTemplatesService.GetAllEmailTemplates(pageListRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(mappedTemplates, result.Records);
        _emailTemplateRepositoryMock.Verify(repo => repo.GetQueryableInclude(), Times.Once());
        _mapperMock.Verify(mapper => mapper.Map<List<EmailTemplatesResponseDto>>(It.IsAny<List<EmailTemplete>>()), Times.Once());
    }

    [Fact]
    public void GetAllEmailTemplates_SortByValidColumnAscending_ReturnsSortedTemplates()
    {
        // Arrange
        var pageListRequest = new PageListRequest
        {
            SortColumn = "Title",
            SortDescending = false
        };

        var emailTemplates = new List<EmailTemplete>
        {
            new EmailTemplete { Id = 1, Title = "TemplateB", IsDeleted = false },
            new EmailTemplete { Id = 2, Title = "TemplateA", IsDeleted = false }
        }.AsQueryable();

        var mappedTemplates = new List<EmailTemplatesResponseDto>
        {
            new EmailTemplatesResponseDto { Id = 2, Title = "TemplateA" },
            new EmailTemplatesResponseDto { Id = 1, Title = "TemplateB" }
        };

        _emailTemplateRepositoryMock
            .Setup(repo => repo.GetQueryableInclude())
            .Returns(emailTemplates);

        _mapperMock
            .Setup(mapper => mapper.Map<List<EmailTemplatesResponseDto>>(It.IsAny<List<EmailTemplete>>()))
            .Returns(mappedTemplates);

        // Act
        var result = _emailTemplatesService.GetAllEmailTemplates(pageListRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalRecords);
        Assert.Equal("TemplateA", result.Records[0].Title);
        Assert.Equal("TemplateB", result.Records[1].Title);
        _emailTemplateRepositoryMock.Verify(repo => repo.GetQueryableInclude(), Times.Once());
        _mapperMock.Verify(mapper => mapper.Map<List<EmailTemplatesResponseDto>>(It.IsAny<List<EmailTemplete>>()), Times.Once());
    }

    [Fact]
    public void GetAllEmailTemplates_SortByValidColumnDescending_ReturnsSortedTemplates()
    {
        // Arrange
        var pageListRequest = new PageListRequest
        {
            SortColumn = "Title",
            SortDescending = true
        };

        var emailTemplates = new List<EmailTemplete>
        {
            new EmailTemplete { Id = 1, Title = "TemplateB", IsDeleted = false },
            new EmailTemplete { Id = 2, Title = "TemplateA", IsDeleted = false }
        }.AsQueryable();

        var mappedTemplates = new List<EmailTemplatesResponseDto>
        {
            new EmailTemplatesResponseDto { Id = 1, Title = "TemplateB" },
            new EmailTemplatesResponseDto { Id = 2, Title = "TemplateA" }
        };

        _emailTemplateRepositoryMock
            .Setup(repo => repo.GetQueryableInclude())
            .Returns(emailTemplates);

        _mapperMock
            .Setup(mapper => mapper.Map<List<EmailTemplatesResponseDto>>(It.IsAny<List<EmailTemplete>>()))
            .Returns(mappedTemplates);

        // Act
        var result = _emailTemplatesService.GetAllEmailTemplates(pageListRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalRecords);
        Assert.Equal("TemplateB", result.Records[0].Title);
        Assert.Equal("TemplateA", result.Records[1].Title);
        _emailTemplateRepositoryMock.Verify(repo => repo.GetQueryableInclude(), Times.Once());
        _mapperMock.Verify(mapper => mapper.Map<List<EmailTemplatesResponseDto>>(It.IsAny<List<EmailTemplete>>()), Times.Once());
    }

    [Fact]
    public void GetAllEmailTemplates_SortByBooleanColumn_InvertsSortDirection()
    {
        // Arrange
        var pageListRequest = new PageListRequest
        {
            SortColumn = "IsDeleted",
            SortDescending = true // Should be inverted to ascending
        };

        var emailTemplates = new List<EmailTemplete>
        {
            new EmailTemplete { Id = 1, Title = "Template1", IsDeleted = false },
            new EmailTemplete { Id = 2, Title = "Template2", IsDeleted = false }
        }.AsQueryable();

        var mappedTemplates = new List<EmailTemplatesResponseDto>
        {
            new EmailTemplatesResponseDto { Id = 1, Title = "Template1" },
            new EmailTemplatesResponseDto { Id = 2, Title = "Template2" }
        };

        _emailTemplateRepositoryMock
            .Setup(repo => repo.GetQueryableInclude())
            .Returns(emailTemplates);

        _mapperMock
            .Setup(mapper => mapper.Map<List<EmailTemplatesResponseDto>>(It.IsAny<List<EmailTemplete>>()))
            .Returns(mappedTemplates);

        // Act
        var result = _emailTemplatesService.GetAllEmailTemplates(pageListRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalRecords);
        _emailTemplateRepositoryMock.Verify(repo => repo.GetQueryableInclude(), Times.Once());
        _mapperMock.Verify(mapper => mapper.Map<List<EmailTemplatesResponseDto>>(It.IsAny<List<EmailTemplete>>()), Times.Once());
    }

    [Fact]
    public void GetAllEmailTemplates_InvalidSortColumn_ThrowsArgumentException()
    {
        // Arrange
        var pageListRequest = new PageListRequest
        {
            SortColumn = "InvalidColumn",
            SortDescending = false
        };

        var emailTemplates = new List<EmailTemplete>().AsQueryable();

        _emailTemplateRepositoryMock
            .Setup(repo => repo.GetQueryableInclude())
            .Returns(emailTemplates);

        // Act & Assert
        var exception = Assert.Throws<AppException>(() => _emailTemplatesService.GetAllEmailTemplates(pageListRequest));
        Assert.Equal("Invalid sort column 'InvalidColumn'. The property does not exist.", exception.Message);
        Assert.Equal(400, exception.StatusCode);
        _emailTemplateRepositoryMock.Verify(repo => repo.GetQueryableInclude(), Times.Once());
        _mapperMock.Verify(mapper => mapper.Map<List<EmailTemplatesResponseDto>>(It.IsAny<List<EmailTemplete>>()), Times.Never());
    }

    [Fact]
    public void GetAllEmailTemplates_NoTemplates_ReturnsEmptyList()
    {
        // Arrange
        var pageListRequest = new PageListRequest
        {
            SortColumn = null,
            SortDescending = false
        };

        var emailTemplates = new List<EmailTemplete>().AsQueryable();

        _emailTemplateRepositoryMock
            .Setup(repo => repo.GetQueryableInclude())
            .Returns(emailTemplates);

        _mapperMock
            .Setup(mapper => mapper.Map<List<EmailTemplatesResponseDto>>(It.IsAny<List<EmailTemplete>>()))
            .Returns(new List<EmailTemplatesResponseDto>());

        // Act
        var result = _emailTemplatesService.GetAllEmailTemplates(pageListRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Records);
        Assert.Equal(0, result.TotalRecords);
        _emailTemplateRepositoryMock.Verify(repo => repo.GetQueryableInclude(), Times.Once());
        _mapperMock.Verify(mapper => mapper.Map<List<EmailTemplatesResponseDto>>(It.IsAny<List<EmailTemplete>>()), Times.Once());
    }
}