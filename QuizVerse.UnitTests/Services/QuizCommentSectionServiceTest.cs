using Moq;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Interface;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using QuizVerse.Infrastructure.Common.Helper;
using Xunit;
using QuizVerse.Infrastructure.Common;

namespace QuizVerse.UnitTests.Services;

public class QuizCommentSectionServiceTests
{
    private readonly Mock<IGenericRepository<QuizRating>> _mockQuizRatingRepository;
    private readonly Mock<IGenericRepository<Quiz>> _mockQuizRepository;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly QuizCommentSectionService _quizCommentService;

    public QuizCommentSectionServiceTests()
    {
        _mockQuizRatingRepository = new Mock<IGenericRepository<QuizRating>>();
        _mockQuizRepository = new Mock<IGenericRepository<Quiz>>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        // Setup HttpContext with user
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.UserData, "123")
        }));
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        _quizCommentService = new QuizCommentSectionService(
            _mockQuizRatingRepository.Object,
            _mockQuizRepository.Object,
            _mockHttpContextAccessor.Object);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithValidQuizIdAndBatchNumber_ReturnsCommentsForBatch()
    {
        // Arrange
        var quizId = 1;
        var batchNumber = 1;
        var mockComments = new List<QuizRating>
        {
            new QuizRating
            {
                Id = 1,
                QuizId = quizId,
                UserId = 123,
                IsFlagged = false,
                Feedback = "Great quiz!",
                QuizRating1 = 5,
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                User = new User
                {
                    Id = 123,
                    UserName = "user1",
                    FullName = "User One",
                    ProfilePic = "profile1.jpg",
                    IsDeleted = false
                }
            },
            new QuizRating
            {
                Id = 2,
                QuizId = quizId,
                UserId = 456,
                IsFlagged = false,
                Feedback = "Very informative",
                QuizRating1 = 4,
                CreatedDate = DateTime.UtcNow.AddDays(-2),
                User = new User
                {
                    Id = 456,
                    UserName = "user2",
                    FullName = "User Two",
                    ProfilePic = "profile2.jpg",
                    IsDeleted = false
                }
            },
            new QuizRating
            {
                Id = 3,
                QuizId = quizId,
                UserId = 789,
                IsFlagged = false,
                Feedback = "Awesome!",
                QuizRating1 = 5,
                CreatedDate = DateTime.UtcNow.AddDays(-3),
                User = new User
                {
                    Id = 789,
                    UserName = "user3",
                    FullName = "User Three",
                    ProfilePic = "profile3.jpg",
                    IsDeleted = false
                }
            },
            new QuizRating
            {
                Id = 4,
                QuizId = quizId,
                UserId = 101,
                IsFlagged = false,
                Feedback = "Good one!",
                QuizRating1 = 4,
                CreatedDate = DateTime.UtcNow.AddDays(-4),
                User = new User
                {
                    Id = 101,
                    UserName = "user4",
                    FullName = "User Four",
                    ProfilePic = "profile4.jpg",
                    IsDeleted = false
                }
            }
        };

        var mockQueryable = mockComments.AsQueryable();

        _mockQuizRepository
            .Setup(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync(true);

        _mockQuizRatingRepository
            .Setup(x => x.GetQueryableInclude())
            .Returns(mockQueryable);

        // Act
        var result = await _quizCommentService.GetCommentsByQuizId(quizId, batchNumber);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Comments);
        Assert.Equal(4, result.Comments.Count); 
        Assert.False(result.hasMoreComments); 

        var firstComment = result.Comments[0];
        Assert.Equal("user1", firstComment.UserName);
        Assert.Equal("User One", firstComment.FullName);
        Assert.Equal("Great quiz!", firstComment.CommentText);
        Assert.Equal(5, firstComment.Rating);
        Assert.True(firstComment.isUser); 

        var secondComment = result.Comments[1];
        Assert.False(secondComment.isUser); 

        _mockQuizRepository.Verify(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()), Times.Once);
        _mockQuizRatingRepository.Verify(x => x.GetQueryableInclude(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithBatchNumber2_ReturnsNextFourComments()
    {
        // Arrange
        var quizId = 1;
        var batchNumber = 2;
        var mockComments = CreateMockComments(8, quizId);

        var mockQueryable = mockComments.AsQueryable();

        _mockQuizRepository
            .Setup(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync(true);

        _mockQuizRatingRepository
            .Setup(x => x.GetQueryableInclude())
            .Returns(mockQueryable);

        // Act
        var result = await _quizCommentService.GetCommentsByQuizId(quizId, batchNumber);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Comments);
        Assert.Equal(4, result.Comments.Count); 
        Assert.False(result.hasMoreComments); 

        Assert.Equal("user5", result.Comments[0].UserName);
        Assert.Equal("user6", result.Comments[1].UserName);
        Assert.Equal("user7", result.Comments[2].UserName);
        Assert.Equal("user8", result.Comments[3].UserName);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithBatchNumberBeyondAvailableComments_ReturnsEmptyList()
    {
        // Arrange
        var quizId = 1;
        var batchNumber = 3; 
        var mockComments = CreateMockComments(8, quizId);

        var mockQueryable = mockComments.AsQueryable();

        _mockQuizRepository
            .Setup(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync(true);

        _mockQuizRatingRepository
            .Setup(x => x.GetQueryableInclude())
            .Returns(mockQueryable);

        // Act
        var result = await _quizCommentService.GetCommentsByQuizId(quizId, batchNumber);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Comments);
        Assert.Empty(result.Comments);
        Assert.False(result.hasMoreComments);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithBatchNumberZero_ThrowsAppException()
    {
        // Arrange
        var quizId = 1;
        var batchNumber = 0;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _quizCommentService.GetCommentsByQuizId(quizId, batchNumber));

        Assert.Equal(Constants.WRONG_BATCH_NUMBER, exception.Message);
        _mockQuizRepository.Verify(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithNegativeBatchNumber_ThrowsAppException()
    {
        // Arrange
        var quizId = 1;
        var batchNumber = -1;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _quizCommentService.GetCommentsByQuizId(quizId, batchNumber));

        Assert.Equal(Constants.WRONG_BATCH_NUMBER, exception.Message);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithNonExistentQuizId_ThrowsAppException()
    {
        // Arrange
        var quizId = 999;
        var batchNumber = 1;

        _mockQuizRepository
            .Setup(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _quizCommentService.GetCommentsByQuizId(quizId, batchNumber));

        Assert.Equal(Constants.QUIZ_NOT_FOUND, exception.Message);
        _mockQuizRepository.Verify(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()), Times.Once);
        _mockQuizRatingRepository.Verify(x => x.GetQueryableInclude(), Times.Never);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithNegativeQuizId_ThrowsAppException()
    {
        // Arrange
        var quizId = -1;
        var batchNumber = 1;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _quizCommentService.GetCommentsByQuizId(quizId, batchNumber));

        Assert.Equal(Constants.INVALID_QUIZ_ID, exception.Message);
    }

    [Fact]
    public async Task GetCommentsByQuizId_ReturnsCommentsOrderedByDateDescending()
    {
        // Arrange
        var quizId = 1;
        var batchNumber = 1;
        var mockComments = new List<QuizRating>
        {
            new QuizRating
            {
                Id = 1,
                QuizId = quizId,
                UserId = 1,
                IsFlagged = false,
                Feedback = "Older comment",
                QuizRating1 = 3,
                CreatedDate = DateTime.UtcNow.AddDays(-5),
                User = new User { UserName = "user1", FullName = "User One", IsDeleted = false }
            },
            new QuizRating
            {
                Id = 2,
                QuizId = quizId,
                UserId = 2,
                IsFlagged = false,
                Feedback = "Newer comment",
                QuizRating1 = 5,
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                User = new User { UserName = "user2", FullName = "User Two", IsDeleted = false }
            }
        };

        var mockQueryable = mockComments.AsQueryable();

        _mockQuizRepository
            .Setup(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync(true);

        _mockQuizRatingRepository
            .Setup(x => x.GetQueryableInclude())
            .Returns(mockQueryable);

        // Act
        var result = await _quizCommentService.GetCommentsByQuizId(quizId, batchNumber);

        // Assert - Should return newest comment first
        Assert.NotNull(result);
        Assert.Equal(2, result.Comments.Count);
        Assert.Equal("user2", result.Comments[0].UserName); // Newest comment first
        Assert.Equal("user1", result.Comments[1].UserName); // Older comment second
    }

    [Fact]
    public async Task GetCommentsByQuizId_HasMoreComments_WhenMoreCommentsExist()
    {
        // Arrange
        var quizId = 1;
        var batchNumber = 1;
        var mockComments = CreateMockComments(5, quizId); 

        var mockQueryable = mockComments.AsQueryable();

        _mockQuizRepository
            .Setup(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync(true);

        _mockQuizRatingRepository
            .Setup(x => x.GetQueryableInclude())
            .Returns(mockQueryable);

        // Act
        var result = await _quizCommentService.GetCommentsByQuizId(quizId, batchNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Comments.Count); 
        Assert.True(result.hasMoreComments); 
    }

    [Fact]
    public async Task GetCommentsByQuizId_NoMoreComments_WhenAllCommentsReturned()
    {
        // Arrange
        var quizId = 1;
        var batchNumber = 1;
        var mockComments = CreateMockComments(4, quizId); 

        var mockQueryable = mockComments.AsQueryable();

        _mockQuizRepository
            .Setup(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync(true);

        _mockQuizRatingRepository
            .Setup(x => x.GetQueryableInclude())
            .Returns(mockQueryable);

        // Act
        var result = await _quizCommentService.GetCommentsByQuizId(quizId, batchNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Comments.Count); 
        Assert.False(result.hasMoreComments); 
    }

    [Fact]
    public async Task TotalCommentsByQuizId_WithValidQuizId_ReturnsCorrectCount()
    {
        // Arrange
        var quizId = 1;
        var mockComments = CreateMockComments(5, quizId);

        var mockQueryable = mockComments.AsQueryable();

        _mockQuizRepository
            .Setup(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync(true);

        _mockQuizRatingRepository
            .Setup(x => x.GetQueryableInclude())
            .Returns(mockQueryable);

        // Act
        var result = await _quizCommentService.TotalCommentsByQuizId(quizId);

        // Assert
        Assert.Equal(5, result);
        _mockQuizRepository.Verify(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task TotalCommentsByQuizId_WithNonExistentQuizId_ThrowsAppException()
    {
        // Arrange
        var quizId = 999;

        _mockQuizRepository
            .Setup(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _quizCommentService.TotalCommentsByQuizId(quizId));

        Assert.Equal(Constants.QUIZ_NOT_FOUND, exception.Message);
    }

    [Fact]
    public async Task TotalCommentsByQuizId_WithNegativeQuizId_ThrowsAppException()
    {
        // Arrange
        var quizId = -1;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _quizCommentService.TotalCommentsByQuizId(quizId));

        Assert.Equal(Constants.INVALID_QUIZ_ID, exception.Message);
    }

    private List<QuizRating> CreateMockComments(int count, int quizId)
    {
        var comments = new List<QuizRating>();
        for (int i = 1; i <= count; i++)
        {
            comments.Add(new QuizRating
            {
                Id = i,
                QuizId = quizId,
                UserId = 100 + i,
                IsFlagged = false,
                Feedback = $"Comment {i}",
                QuizRating1 = 4,
                CreatedDate = DateTime.UtcNow.AddDays(-i),
                User = new User
                {
                    Id = 100 + i,
                    UserName = $"user{i}",
                    FullName = $"User {i}",
                    ProfilePic = $"profile{i}.jpg",
                    IsDeleted = false
                }
            });
        }
        return comments;
    }
}