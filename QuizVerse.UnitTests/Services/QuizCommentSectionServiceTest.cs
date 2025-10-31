using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class QuizCommentSectionServiceTests
{
    private readonly Mock<IGenericRepository<QuizRating>> _mockQuizRatingRepository;
    private readonly Mock<IGenericRepository<Quiz>> _mockQuizRepository;
    private readonly QuizCommentSectionService _quizCommentService;

    public QuizCommentSectionServiceTests()
    {
        _mockQuizRatingRepository = new Mock<IGenericRepository<QuizRating>>();
        _mockQuizRepository = new Mock<IGenericRepository<Quiz>>();
        _quizCommentService = new QuizCommentSectionService(
            _mockQuizRatingRepository.Object,
            _mockQuizRepository.Object);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithValidQuizId_ReturnsComments()
    {
        // Arrange
        var quizId = 1;
        var mockComments = new List<QuizRating>
        {
            new QuizRating
            {
                Id = 1,
                QuizId = quizId,
                IsFlagged = false,
                Feedback = "Great quiz!",
                QuizRating1 = 5,
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                User = new User
                {
                    UserName = "user1",
                    ProfilePic = "profile1.jpg",
                    IsDeleted = false
                }
            },
            new QuizRating
            {
                Id = 2,
                QuizId = quizId,
                IsFlagged = false,
                Feedback = "Very informative",
                QuizRating1 = 4,
                CreatedDate = DateTime.UtcNow.AddDays(-2),
                User = new User
                {
                    UserName = "user2",
                    ProfilePic = "profile2.jpg",
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
        var result = await _quizCommentService.GetCommentsByQuizId(quizId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var firstComment = result[0];
        Assert.Equal("user1", firstComment.UserName);
        Assert.Equal("profile1.jpg", firstComment.ProfilePic);
        Assert.Equal("Great quiz!", firstComment.CommentText);
        Assert.Equal(5, firstComment.Rating);

        var secondComment = result[1];
        Assert.Equal("user2", secondComment.UserName);
        Assert.Equal("profile2.jpg", secondComment.ProfilePic);
        Assert.Equal("Very informative", secondComment.CommentText);
        Assert.Equal(4, secondComment.Rating);

        _mockQuizRepository.Verify(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()), Times.Once);
        _mockQuizRatingRepository.Verify(x => x.GetQueryableInclude(), Times.Once);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithNonExistentQuizId_ThrowsAppException()
    {
        // Arrange
        var quizId = 999;

        _mockQuizRepository
            .Setup(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _quizCommentService.GetCommentsByQuizId(quizId));

        Assert.Equal("Quiz not found.", exception.Message);
        _mockQuizRepository.Verify(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()), Times.Once);
        _mockQuizRatingRepository.Verify(x => x.GetQueryableInclude(), Times.Never);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithDeletedQuiz_ThrowsAppException()
    {
        // Arrange
        var quizId = 1;

        _mockQuizRepository
            .Setup(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _quizCommentService.GetCommentsByQuizId(quizId));

        Assert.Equal("Quiz not found.", exception.Message);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithFlaggedComments_ExcludesFlaggedComments()
    {
        // Arrange
        var quizId = 1;
        var mockComments = new List<QuizRating>
        {
            new QuizRating
            {
                Id = 1,
                QuizId = quizId,
                IsFlagged = false, // Not flagged - should be included
                Feedback = "Good quiz",
                QuizRating1 = 4,
                CreatedDate = DateTime.UtcNow,
                User = new User
                {
                    UserName = "user1",
                    ProfilePic = "profile1.jpg",
                    IsDeleted = false
                }
            },
            new QuizRating
            {
                Id = 2,
                QuizId = quizId,
                IsFlagged = true, // Flagged - should be excluded
                Feedback = "Inappropriate comment",
                QuizRating1 = 1,
                CreatedDate = DateTime.UtcNow,
                User = new User
                {
                    UserName = "user2",
                    ProfilePic = "profile2.jpg",
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
        var result = await _quizCommentService.GetCommentsByQuizId(quizId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("user1", result[0].UserName);
        Assert.Equal("Good quiz", result[0].CommentText);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithDeletedUsers_ExcludesDeletedUsersComments()
    {
        // Arrange
        var quizId = 1;
        var mockComments = new List<QuizRating>
        {
            new QuizRating
            {
                Id = 1,
                QuizId = quizId,
                IsFlagged = false,
                Feedback = "Active user comment",
                QuizRating1 = 5,
                CreatedDate = DateTime.UtcNow,
                User = new User
                {
                    UserName = "activeuser",
                    ProfilePic = "active.jpg",
                    IsDeleted = false // Not deleted - should be included
                }
            },
            new QuizRating
            {
                Id = 2,
                QuizId = quizId,
                IsFlagged = false,
                Feedback = "Deleted user comment",
                QuizRating1 = 3,
                CreatedDate = DateTime.UtcNow,
                User = new User
                {
                    UserName = "deleteduser",
                    ProfilePic = "deleted.jpg",
                    IsDeleted = true // Deleted - should be excluded
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
        var result = await _quizCommentService.GetCommentsByQuizId(quizId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("activeuser", result[0].UserName);
        Assert.Equal("Active user comment", result[0].CommentText);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithNullFeedback_IncludesCommentsWithEmptyFeedback()
    {
        // Arrange
        var quizId = 1;
        var mockComments = new List<QuizRating>
        {
            new QuizRating
            {
                Id = 1,
                QuizId = quizId,
                IsFlagged = false,
                Feedback = null, // Null feedback - should become empty string
                QuizRating1 = 5,
                CreatedDate = DateTime.UtcNow,
                User = new User
                {
                    UserName = "user1",
                    ProfilePic = "profile1.jpg",
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
        var result = await _quizCommentService.GetCommentsByQuizId(quizId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("", result[0].CommentText); // Should be empty string due to null-coalescing
        Assert.Equal("user1", result[0].UserName);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithNoComments_ReturnsEmptyList()
    {
        // Arrange
        var quizId = 1;
        var mockComments = new List<QuizRating>();

        var mockQueryable = mockComments.AsQueryable();

        _mockQuizRepository
            .Setup(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync(true);

        _mockQuizRatingRepository
            .Setup(x => x.GetQueryableInclude())
            .Returns(mockQueryable);

        // Act
        var result = await _quizCommentService.GetCommentsByQuizId(quizId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithMultipleQuizzes_ReturnsOnlyRequestedQuizComments()
    {
        // Arrange
        var quizId1 = 1;
        var quizId2 = 2;
        var mockComments = new List<QuizRating>
        {
            new QuizRating
            {
                Id = 1,
                QuizId = quizId1, // Requested quiz
                IsFlagged = false,
                Feedback = "Quiz 1 comment",
                QuizRating1 = 5,
                CreatedDate = DateTime.UtcNow,
                User = new User
                {
                    UserName = "user1",
                    ProfilePic = "profile1.jpg",
                    IsDeleted = false
                }
            },
            new QuizRating
            {
                Id = 2,
                QuizId = quizId2, // Different quiz - should be excluded
                IsFlagged = false,
                Feedback = "Quiz 2 comment",
                QuizRating1 = 4,
                CreatedDate = DateTime.UtcNow,
                User = new User
                {
                    UserName = "user2",
                    ProfilePic = "profile2.jpg",
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
        var result = await _quizCommentService.GetCommentsByQuizId(quizId1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Quiz 1 comment", result[0].CommentText);
        Assert.Equal("user1", result[0].UserName);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithNullUserProperties_HandlesGracefully()
    {
        // Arrange
        var quizId = 1;
        var mockComments = new List<QuizRating>
        {
            new QuizRating
            {
                Id = 1,
                QuizId = quizId,
                IsFlagged = false,
                Feedback = "Valid comment",
                QuizRating1 = 5,
                CreatedDate = DateTime.UtcNow,
                User = new User
                {
                    UserName = null, // Null username
                    ProfilePic = null, // Null profile pic
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
        var result = await _quizCommentService.GetCommentsByQuizId(quizId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Null(result[0].UserName); // Null username remains null
        Assert.Null(result[0].ProfilePic); // Null profile pic remains null
        Assert.Equal("Valid comment", result[0].CommentText);
    }

    [Fact]
    public async Task GetCommentsByQuizId_VerifyQueryStructure()
    {
        // Arrange
        var quizId = 1;
        var mockComments = new List<QuizRating>().AsQueryable();

        _mockQuizRepository
            .Setup(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync(true);

        _mockQuizRatingRepository
            .Setup(x => x.GetQueryableInclude())
            .Returns(mockComments);

        // Act
        var result = await _quizCommentService.GetCommentsByQuizId(quizId);

        // Assert
        Assert.NotNull(result);
        _mockQuizRatingRepository.Verify(x => x.GetQueryableInclude(), Times.Once);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithZeroQuizId_ThrowsAppException()
    {
        // Arrange
        var quizId = 0;

        _mockQuizRepository
            .Setup(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _quizCommentService.GetCommentsByQuizId(quizId));

        Assert.Equal("Quiz not found.", exception.Message);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithNegativeQuizId_ThrowsAppException()
    {
        // Arrange
        var quizId = -1;

        _mockQuizRepository
            .Setup(x => x.Exists(It.IsAny<System.Linq.Expressions.Expression<Func<Quiz, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            _quizCommentService.GetCommentsByQuizId(quizId));

        Assert.Equal("Quiz not found.", exception.Message);
    }

    [Fact]
    public async Task GetCommentsByQuizId_WithEmptyFeedback_ReturnsEmptyString()
    {
        // Arrange
        var quizId = 1;
        var mockComments = new List<QuizRating>
        {
            new QuizRating
            {
                Id = 1,
                QuizId = quizId,
                IsFlagged = false,
                Feedback = "", // Empty string feedback
                QuizRating1 = 4,
                CreatedDate = DateTime.UtcNow,
                User = new User
                {
                    UserName = "user1",
                    ProfilePic = "profile1.jpg",
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
        var result = await _quizCommentService.GetCommentsByQuizId(quizId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("", result[0].CommentText); // Should preserve empty string
        Assert.Equal("user1", result[0].UserName);
    }
}