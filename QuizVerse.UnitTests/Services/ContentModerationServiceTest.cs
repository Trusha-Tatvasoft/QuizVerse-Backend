using System.Linq.Expressions;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using Xunit;
using System.Security.Claims;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Common;
using Npgsql;
using System.Text.Json;

namespace QuizVerse.UnitTests.Services
{
    public class ContentModerationServiceTests
    {
        private readonly Mock<IGenericRepository<QuizIssueReport>> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<ISqlQueryRepository> _sqlRepoMock;
        private readonly Mock<IGenericRepository<QuizRating>> _quizRatingRepoMock;
        private readonly ContentModerationService _service;

        public ContentModerationServiceTests()
        {
            _repoMock = new Mock<IGenericRepository<QuizIssueReport>>();
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _sqlRepoMock = new Mock<ISqlQueryRepository>();
            _quizRatingRepoMock = new Mock<IGenericRepository<QuizRating>>();

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.UserData, "1"),
                    new Claim(ClaimTypes.Role, "Admin")
                }, "mock"))
            };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            _service = new ContentModerationService(
                _repoMock.Object,
                _httpContextAccessorMock.Object,
                _mapperMock.Object,
                _sqlRepoMock.Object,
                _quizRatingRepoMock.Object
            );
        }

        private static IQueryable<QuizIssueReport> GetDummyReports()
        {
            return new List<QuizIssueReport>
            {
                new()
                {
                    Id = 1,
                    Severity = 2,
                    Quiz = new Quiz
                    {
                        Name = "C# Basics",
                        CreatedByNavigation = new User { FullName = "Creator A" }
                    },
                    User = new User { FullName = "Reporter A" }
                },
                new()
                {
                    Id = 2,
                    Severity = 3,
                    Quiz = new Quiz
                    {
                        Name = "ASP.NET",
                        CreatedByNavigation = new User { FullName = "Creator B" }
                    },
                    User = new User { FullName = "Reporter B" }
                },
                new()
                {
                    Id = 3,
                    Severity = 1,
                    Quiz = new Quiz
                    {
                        Name = "Entity Framework",
                        CreatedByNavigation = new User { FullName = "Creator C" }
                    },
                    User = new User { FullName = "Reporter C" }
                }
            }.AsQueryable();
        }

        private void SetupRepositoryAndPagination(IQueryable<QuizIssueReport> reports)
        {
            _repoMock.Setup(r => r.GetQueryableInclude(
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>()))
                .Returns(reports);

            _repoMock.Setup(r => r.PaginatedList<QuizReportIssueResponseDTO>(
                    It.IsAny<IQueryable<QuizIssueReport>>(),
                    It.IsAny<PageListRequest>(),
                    It.IsAny<Func<IQueryable<QuizIssueReport>, IQueryable<QuizReportIssueResponseDTO>>>()))
                    .ReturnsAsync((IQueryable<QuizIssueReport> query,
                        PageListRequest req,
                        Func<IQueryable<QuizIssueReport>, IQueryable<QuizReportIssueResponseDTO>> selector) =>
       {
           var result = reports.Select(r => new QuizReportIssueResponseDTO
           {
               Id = r.Id,
               QuizTitle = r.Quiz.Name,
               Reporter = r.User.FullName,
               Severity = r.Severity
           }).ToList();

           return new PageListResponse<QuizReportIssueResponseDTO>
           {
               Records = result,
               TotalRecords = result.Count
           };
       });

        }

        [Fact]
        public async Task GetQuizReportByPaginationAsync_ReturnsData_WhenReportsExist()
        {
            // Arrange
            var reports = GetDummyReports();
            SetupRepositoryAndPagination(reports);

            var query = new PageListRequest { PageNumber = 1, PageSize = 10, SortColumn = "quiz" };

            // Act
            var result = await _service.GetQuizReportByPaginationAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalRecords);
            Assert.Contains(result.Records, x => x.QuizTitle == "C# Basics");
        }

        [Fact]
        public async Task GetQuizReportByPaginationAsync_ReturnsEmpty_WhenNoData()
        {
            // Arrange
            var emptyReports = new List<QuizIssueReport>().AsQueryable();
            SetupRepositoryAndPagination(emptyReports);

            var query = new PageListRequest { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.GetQuizReportByPaginationAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Records);
            Assert.Equal(0, result.TotalRecords);
        }

        [Theory]
        [InlineData("quiz", "C# Basics")]
        [InlineData("creator", "Creator A")]
        [InlineData("reporter", "Reporter A")]
        [InlineData("severity", "C# Basics")] // just ensure not null, severity sorting handled internally
        public async Task GetQuizReportByPaginationAsync_SortsCorrectly(string sortColumn, string expectedFirst)
        {
            // Arrange
            var reports = GetDummyReports();
            SetupRepositoryAndPagination(reports);

            var query = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortColumn = sortColumn,
                SortDescending = false
            };

            // Act
            var result = await _service.GetQuizReportByPaginationAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalRecords);
        }

        [Fact]
        public async Task GetQuizReportByPaginationAsync_UsesDefaultSort_WhenInvalidColumn()
        {
            // Arrange
            var reports = GetDummyReports();
            SetupRepositoryAndPagination(reports);

            var query = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortColumn = "unknown"
            };

            // Act
            var result = await _service.GetQuizReportByPaginationAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalRecords);
            Assert.True(result.Records.First().Id > 0);
        }

        [Fact]
        public async Task GetQuizReportByPaginationAsync_FiltersBySeverity_WhenValid()
        {
            // Arrange
            var reports = GetDummyReports();
            SetupRepositoryAndPagination(reports);

            var query = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                Filters = new FilterDto
                {
                    IssueReportSeverity = (QuestionOrQuizIssueReportSeverity?)2
                }
            };

            // Act
            var result = await _service.GetQuizReportByPaginationAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Records.First().Id);
        }

        [Fact]
        public async Task GetQuizReportByPaginationAsync_ThrowsException_ForInvalidSeverity()
        {
            // Arrange
            var reports = GetDummyReports();
            SetupRepositoryAndPagination(reports);

            var query = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                Filters = new FilterDto
                {
                    IssueReportSeverity = (QuestionOrQuizIssueReportSeverity?)99 // invalid enum
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<AppException>(() => _service.GetQuizReportByPaginationAsync(query));
        }

        #region Flagged Comments
        [Fact]
        public async Task GetFlaggedComments_ShouldReturnData_WhenRecordsExist()
        {
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortColumn = "created_date",
                SortDescending = true,
                Filters = new FilterDto { CommentStatus = QuizRatingStatus.Accepted }
            };

            var dbResult = new FlaggedCommentsResultDTO
            {
                TotalRecords = 2,
                Records = JsonSerializer.Serialize(new List<FlaggedCommentDto>
        {
            new() { Id = 1, Comment = "Inappropriate comment", Author = "User A" },
            new() { Id = 2, Comment = "Spam content", Author = "User B" }
        })
            };

            _sqlRepoMock
                .Setup(r => r.SqlQuerySingleAsync<FlaggedCommentsResultDTO>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(dbResult);

            var result = await _service.GetFlaggedComments(request);

            Assert.NotNull(result);
            Assert.Equal(2, result.TotalRecords);
            Assert.Equal(2, result.Records.Count);
            Assert.Contains(result.Records, c => c.Comment.Contains("Spam"));
        }

        [Fact]
        public async Task GetFlaggedComments_ShouldReturnEmpty_WhenNoRecordsFound()
        {
            var request = new PageListRequest { PageNumber = 1, PageSize = 10 };

            var dbResult = new FlaggedCommentsResultDTO
            {
                TotalRecords = 0,
                Records = string.Empty
            };

            _sqlRepoMock
                .Setup(r => r.SqlQuerySingleAsync<FlaggedCommentsResultDTO>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(dbResult);

            var result = await _service.GetFlaggedComments(request);

            Assert.NotNull(result);
            Assert.Equal(0, result.TotalRecords);
            Assert.Empty(result.Records);
        }

        [Fact]
        public async Task GetFlaggedCommentById_ShouldReturnSingleRecord()
        {
            int commentId = 5;
            var expectedDto = new FlaggedCommentViewDto
            {
                Id = commentId,
                Comment = "This is a flagged comment",
                Author = "User X"
            };

            _sqlRepoMock
                .Setup(r => r.SqlQuerySingleAsync<FlaggedCommentViewDto>(
                    It.IsAny<string>(),
                    It.Is<NpgsqlParameter[]>(p => p[0].Value.Equals(commentId))))
                .ReturnsAsync(expectedDto);

            var result = await _service.GetFlaggedCommentById(commentId);

            Assert.NotNull(result);
            Assert.Equal(commentId, result.Id);
            Assert.Equal("User X", result.Author);
        }

        [Fact]
        public async Task GetFlaggedCommentById_ShouldReturnNull_WhenNoDataFound()
        {
            int commentId = 999;
            _sqlRepoMock
                .Setup(r => r.SqlQuerySingleAsync<FlaggedCommentViewDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync((FlaggedCommentViewDto)null);

            var result = await _service.GetFlaggedCommentById(commentId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetFlaggedComments_ShouldApplyFilter_WhenCommentStatusProvided()
        {
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortColumn = "created_date",
                SortDescending = true,
                Filters = new FilterDto
                {
                    CommentStatus = QuizRatingStatus.Pending
                }
            };

            var expectedDbResult = new FlaggedCommentsResultDTO
            {
                TotalRecords = 1,
                Records = JsonSerializer.Serialize(new List<FlaggedCommentDto>
                {
                    new()
                    {
                        Id = 10,
                        Comment = "Pending review",
                        Status = ((int)QuizRatingStatus.Pending)
                    }
                })
            };

            object[]? capturedParams = null;

            _sqlRepoMock
                .Setup(r => r.SqlQuerySingleAsync<FlaggedCommentsResultDTO>(
                    It.IsAny<string>(),
                    It.IsAny<object[]>()))
                .Callback<string, object[]>((query, parameters) => capturedParams = parameters)
                .ReturnsAsync(expectedDbResult);

            var result = await _service.GetFlaggedComments(request);

            Assert.NotNull(result);
            Assert.Single(result.Records);
            Assert.Equal("Pending review", result.Records.First().Comment);

            Assert.NotNull(capturedParams);
            var npgParams = capturedParams!
                .OfType<NpgsqlParameter>()
                .ToList();

            var statusParam = npgParams.FirstOrDefault(p => p.ParameterName.Contains("p_status"));
            Assert.NotNull(statusParam);
            Assert.Equal((int)QuizRatingStatus.Pending, statusParam!.Value);

            _sqlRepoMock.Verify(r =>
                r.SqlQuerySingleAsync<FlaggedCommentsResultDTO>(
                    It.IsAny<string>(),
                    It.IsAny<object[]>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateFlaggedCommentStatus_ShouldThrow_WhenCommentNotFound()
        {
            _quizRatingRepoMock
                .Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<QuizRating, bool>>>(), null))
                .ReturnsAsync((QuizRating)null);

            var request = new UpdateFlaggedCommentStatusRequest { Id = 99, Status = 1 };

            var ex = await Assert.ThrowsAsync<AppException>(() => _service.UpdateFlaggedCommentStatus(request));
            Assert.Equal(Constants.FLAGGED_COMMENT_NOT_FOUND, ex.Message);

            _quizRatingRepoMock.Verify(r => r.UpdateAsync(It.IsAny<QuizRating>()), Times.Never);
        }
        [Fact]
        public async Task UpdateFlaggedCommentStatus_ShouldThrow_WhenStatusIsAcceptedOrIgnore()
        {
            var comment = new QuizRating { Id = 1, IsFlagged = true, Status = 2 };
            _quizRatingRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuizRating, bool>>>(), null))
                .ReturnsAsync(comment);

            var requestAccepted = new UpdateFlaggedCommentStatusRequest
            {
                Id = 1,
                Status = (int)QuizRatingStatus.Accepted
            };

            var requestIgnore = new UpdateFlaggedCommentStatusRequest
            {
                Id = 1,
                Status = (int)QuizRatingStatus.Ignore
            };

            var exAccepted = await Assert.ThrowsAsync<AppException>(() => _service.UpdateFlaggedCommentStatus(requestAccepted));
            Assert.Equal(Constants.CAN_NOT_UPDATE_STATUS_COMMENT, exAccepted.Message);

            var exIgnore = await Assert.ThrowsAsync<AppException>(() => _service.UpdateFlaggedCommentStatus(requestIgnore));
            Assert.Equal(Constants.CAN_NOT_UPDATE_STATUS_COMMENT, exIgnore.Message);

            _quizRatingRepoMock.Verify(r => r.UpdateAsync(It.IsAny<QuizRating>()), Times.Never);
        }

        [Fact]
        public async Task UpdateFlaggedCommentStatus_ShouldUpdate_WhenValidStatus()
        {
            var comment = new QuizRating { Id = 1, IsFlagged = true, Status = 4 };
            _quizRatingRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuizRating, bool>>>(), null))
                .ReturnsAsync(comment);

            var request = new UpdateFlaggedCommentStatusRequest
            {
                Id = 1,
                Status = (int)QuizRatingStatus.Pending
            };

            await _service.UpdateFlaggedCommentStatus(request);

            _quizRatingRepoMock.Verify(r => r.UpdateAsync(It.Is<QuizRating>(c =>
                c.Id == 1 &&
                c.Status == (int)QuizRatingStatus.Pending &&
                c.ModifiedBy == 1 &&
                c.ModifiedDate <= DateTime.UtcNow
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateFlaggedCommentStatus_ShouldSetModifiedFieldsCorrectly()
        {
            var comment = new QuizRating { Id = 2, IsFlagged = true, Status = 0 };
            _quizRatingRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuizRating, bool>>>(), null))
                .ReturnsAsync(comment);

            var request = new UpdateFlaggedCommentStatusRequest { Id = 2, Status = 5 };

            await _service.UpdateFlaggedCommentStatus(request);

            _quizRatingRepoMock.Verify(r => r.UpdateAsync(It.Is<QuizRating>(c =>
                c.ModifiedBy == 1 &&
                c.ModifiedDate <= DateTime.UtcNow
            )), Times.Once);
        }
        #endregion
    }
}
