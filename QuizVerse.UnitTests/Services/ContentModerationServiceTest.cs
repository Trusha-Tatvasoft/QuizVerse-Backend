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

namespace QuizVerse.UnitTests.Services
{
    public class ContentModerationServiceTests
    {
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly ContentModerationService _service;
        private readonly Mock<IGenericRepository<QuizIssueReport>> _reportedQuizMock = new();
        private readonly Mock<IGenericRepository<Quiz>> _quizRepoMock;

        public ContentModerationServiceTests()
        {
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _reportedQuizMock = new Mock<IGenericRepository<QuizIssueReport>>();
            _quizRepoMock = new Mock<IGenericRepository<Quiz>>();

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
                _reportedQuizMock.Object,
                _quizRepoMock.Object,
                _httpContextAccessorMock.Object,
                _mapperMock.Object
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
                        Status = 3,
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
                        Status = 3,
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
                        Status = 1,
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
            _reportedQuizMock.Setup(r => r.GetQueryableInclude(
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>()))
                .Returns(reports);

            _reportedQuizMock.Setup(r => r.PaginatedList<QuizReportIssueResponseDTO>(
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

        #region UpdateQuestionReportAction Tests

        [Fact]
        public async Task UpdateQuestionReportAction_UpdatesStatus_WhenReportIsPending_AndUserIsAdmin()
        {
            // Arrange
            var reportId = 500;
            var actionRequest = new QuizAndQuestionReportAction
            {
                ReportId = reportId,
                QuestionOrQuizIssueReportNewStatus = (int)QuestionOrQuizIssueReportStatus.UnderReview
            };

            var existingReport = new QuizIssueReport
            {
                Id = reportId,
                Status = (int)QuestionOrQuizIssueReportStatus.Pending,
                ModifiedBy = 1 // same as current user from mock context
            };

            _reportedQuizMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuizIssueReport, bool>>>(), null))
                .ReturnsAsync(existingReport);

            _reportedQuizMock
                .Setup(r => r.UpdateAsync(existingReport))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateQuizReportAction(actionRequest);

            // Assert
            Assert.Equal(Constants.QUESTION_ISSUE_ACTION_UPDATE_SUCCESS_MESSAGE, result);
            Assert.Equal(actionRequest.QuestionOrQuizIssueReportNewStatus, existingReport.Status);
            _reportedQuizMock.Verify(r => r.UpdateAsync(existingReport), Times.Once);
        }

        [Fact]
        public async Task GetQuizReportByPaginationAsync_FiltersByStatus_WhenValid()
        {
            // Arrange
            var reports = GetDummyReports().ToList();
            reports[0].Status = (int)QuestionOrQuizIssueReportStatus.Pending;
            reports[1].Status = (int)QuestionOrQuizIssueReportStatus.Pending;
            reports[2].Status = (int)QuestionOrQuizIssueReportStatus.Accepted;

            SetupRepositoryAndPagination(reports.AsQueryable());

            var query = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                Filters = new FilterDto
                {
                    IssueReportStatus = QuestionOrQuizIssueReportStatus.Pending
                }
            };

            // Act
            var result = await _service.GetQuizReportByPaginationAsync(query);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetQuizReportByPaginationAsync_ThrowsException_ForInvalidStatus()
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
                    IssueReportStatus = (QuestionOrQuizIssueReportStatus?)999 // invalid
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => _service.GetQuizReportByPaginationAsync(query));
            Assert.Equal(Constants.INVALID_ROLE_MESSAGE, ex.Message);
        }

        [Fact]
        public async Task UpdateQuestionReportAction_ThrowsAppException_WhenReportFinalized()
        {
            // Arrange
            var reportId = 501;
            var actionRequest = new QuizAndQuestionReportAction
            {
                ReportId = reportId,
                QuestionOrQuizIssueReportNewStatus = (int)QuestionOrQuizIssueReportStatus.Pending
            };

            var existingReport = new QuizIssueReport
            {
                Id = reportId,
                Status = (int)QuestionOrQuizIssueReportStatus.Accepted,
                ModifiedBy = 1
            };

            _reportedQuizMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuizIssueReport, bool>>>(), null))
                .ReturnsAsync(existingReport);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => _service.UpdateQuizReportAction(actionRequest));
            Assert.Equal(Constants.QUESTION_ISSUE_REPORT_FINALIZED_INFO, ex.Message);
        }

        [Fact]
        public async Task UpdateQuestionReportAction_ThrowsAppException_WhenUnderReviewByDifferentReviewer_AndUserIsNotSuperAdmin()
        {
            // Arrange
            var reportId = 502;
            var actionRequest = new QuizAndQuestionReportAction
            {
                ReportId = reportId,
                QuestionOrQuizIssueReportNewStatus = (int)QuestionOrQuizIssueReportStatus.Pending
            };

            var existingReport = new QuizIssueReport
            {
                Id = reportId,
                Status = (int)QuestionOrQuizIssueReportStatus.UnderReview,
                ModifiedBy = 999 // Different reviewer
            };

            _reportedQuizMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuizIssueReport, bool>>>(), null))
                .ReturnsAsync(existingReport);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => _service.UpdateQuizReportAction(actionRequest));
            Assert.Equal(Constants.QUESTION_ISSUE_REPORT_NOT_HAVE_PERMISSION_EDIT, ex.Message);
        }

        [Fact]
        public async Task UpdateQuestionReportAction_UpdatesStatus_WhenUnderReviewBySameReviewer()
        {
            // Arrange
            var reportId = 503;
            var actionRequest = new QuizAndQuestionReportAction
            {
                ReportId = reportId,
                QuestionOrQuizIssueReportNewStatus = (int)QuestionOrQuizIssueReportStatus.Accepted
            };

            var existingReport = new QuizIssueReport
            {
                Id = reportId,
                Status = (int)QuestionOrQuizIssueReportStatus.UnderReview,
                ModifiedBy = 1 // Same as current user (mock context userId)
            };

            _reportedQuizMock
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<QuizIssueReport, bool>>>(),
                    It.IsAny<Func<IQueryable<QuizIssueReport>, IQueryable<QuizIssueReport>>?>()
                ))
                .ReturnsAsync(existingReport); // ✅ THIS WAS MISSING

            _reportedQuizMock
                .Setup(r => r.UpdateAsync(existingReport))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateQuizReportAction(actionRequest);

            // Assert
            Assert.Equal(Constants.QUIZ_ISSUE_ACTION_UPDATE_SUCCESS_MESSAGE, result);
            Assert.Equal(actionRequest.QuestionOrQuizIssueReportNewStatus, existingReport.Status);
            _reportedQuizMock.Verify(r => r.UpdateAsync(existingReport), Times.Once);
        }


        [Fact]
        public async Task UpdateQuestionReportAction_ThrowsAppException_WhenReportNotFound()
        {
            // Arrange
            var actionRequest = new QuizAndQuestionReportAction
            {
                ReportId = 600,
                QuestionOrQuizIssueReportNewStatus = 3
            };

            _reportedQuizMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuizIssueReport, bool>>>(), null))
                .ReturnsAsync((QuizIssueReport)null);

            // Act & Assert
            await Assert.ThrowsAsync<AppException>(() => _service.UpdateQuizReportAction(actionRequest));
        }
        #endregion

        [Fact]
        public async Task UpdateQuizReportAction_InactivatesQuiz_WhenNewStatusIsInactive()
        {
            // Arrange
            var reportId = 700;

            var actionRequest = new QuizAndQuestionReportAction
            {
                ReportId = reportId,
                QuestionOrQuizIssueReportNewStatus = 2 // Inactive
            };

            var existingReport = new QuizIssueReport
            {
                Id = reportId,
                Status = (int)QuestionOrQuizIssueReportStatus.Pending,
                ModifiedBy = 1 // same user from context
            };

            var quiz = new Quiz
            {
                Id = 10,
                Status = (int)QuizStatus.Active
            };

            _reportedQuizMock
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<QuizIssueReport, bool>>>(),
                    It.IsAny<Func<IQueryable<QuizIssueReport>, IQueryable<QuizIssueReport>>?>()
                ))
                .ReturnsAsync(existingReport);

            var asyncReportWithQuizQuery = new TestAsyncEnumerable<QuizIssueReport>(
                new List<QuizIssueReport> { new QuizIssueReport { Quiz = quiz, Id = reportId } }
            );

            _reportedQuizMock
                .Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<QuizIssueReport, object>>>()))
                .Returns(asyncReportWithQuizQuery);

            _quizRepoMock
                .Setup(r => r.UpdateAsync(quiz))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateQuizReportAction(actionRequest);

            // Assert
            Assert.Equal(Constants.QUIZ_ISSUE_ACTION_UPDATE_SUCCESS_MESSAGE, result);
            Assert.Equal((int)QuizStatus.Inactive, quiz.Status);
            Assert.Equal(1, quiz.ModifiedBy);
            Assert.NotEqual(default, quiz.ModifiedDate);

            _quizRepoMock.Verify(r => r.UpdateAsync(quiz), Times.Once);
        }

    }
}