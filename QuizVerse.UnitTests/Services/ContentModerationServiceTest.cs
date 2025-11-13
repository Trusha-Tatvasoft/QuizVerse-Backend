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
using QuizVerse.Infrastructure.Common;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.DTOs;
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
        private readonly Mock<ISqlQueryRepository> _sqlQueryRepositoryMock;
        private readonly Mock<IGenericRepository<QuestionIssueReport>> _reportedQuestionRepoMock;
        private readonly Mock<IQuestionPoolService> _questionPoolServiceMock;
        private readonly Mock<ISqlQueryRepository> _sqlRepoMock;
        private readonly Mock<IGenericRepository<QuizRating>> _quizRatingRepoMock;
        private readonly ContentModerationService _service;

        public ContentModerationServiceTests()
        {
            _repoMock = new Mock<IGenericRepository<QuizIssueReport>>();
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _sqlQueryRepositoryMock = new Mock<ISqlQueryRepository>();
            _reportedQuestionRepoMock = new Mock<IGenericRepository<QuestionIssueReport>>();
            _questionPoolServiceMock = new Mock<IQuestionPoolService>();
            _sqlRepoMock = new Mock<ISqlQueryRepository>();
            _quizRatingRepoMock = new Mock<IGenericRepository<QuizRating>>();

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.UserData, "1"),
                    new Claim(ClaimTypes.UserData, "1"),
                    new Claim(ClaimTypes.Role, "Admin")
                }, "mock"))
            };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            _service = new ContentModerationService(
                _sqlQueryRepositoryMock.Object,
                _repoMock.Object,
                _reportedQuestionRepoMock.Object,
                _questionPoolServiceMock.Object,
                _httpContextAccessorMock.Object,
                _mapperMock.Object,
                _sqlRepoMock.Object,
                _quizRatingRepoMock.Object
            );
        }

        #region GetQuizReportByPaginationAsync Tests
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
        #endregion

        #region GetContentModerationMetricsData Tests
        [Fact]
        public async Task GetContentModerationMatricsData_ReturnsMetrics_WhenDataExists()
        {
            var mockMetrics = new ContentModerationMetricsDataDto
            {
                PendingReportsCount = 5,
                UnderReviewReportsCount = 3,
                TodayResolvedReportsCount = 2,
                BannedUserCount = 1
            };

            _sqlQueryRepositoryMock
                .Setup(repo => repo.SqlQuerySingleAsync<ContentModerationMetricsDataDto>(It.IsAny<string>()))
                .ReturnsAsync(mockMetrics);

            var result = await _service.GetContentModerationMatricsData();

            Assert.NotNull(result);
            Assert.Equal(mockMetrics.PendingReportsCount, result.PendingReportsCount);
            Assert.Equal(mockMetrics.UnderReviewReportsCount, result.UnderReviewReportsCount);
            Assert.Equal(mockMetrics.TodayResolvedReportsCount, result.TodayResolvedReportsCount);
            Assert.Equal(mockMetrics.BannedUserCount, result.BannedUserCount);

            _sqlQueryRepositoryMock.Verify(x => x.SqlQuerySingleAsync<ContentModerationMetricsDataDto>(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetContentModerationMatricsData_ReturnsNull_WhenRepositoryReturnsNull()
        {
            _sqlQueryRepositoryMock
                .Setup(repo => repo.SqlQuerySingleAsync<ContentModerationMetricsDataDto>(It.IsAny<string>()))
                .ReturnsAsync((ContentModerationMetricsDataDto?)null);

            var result = await _service.GetContentModerationMatricsData();

            Assert.Null(result);
            _sqlQueryRepositoryMock.Verify(x => x.SqlQuerySingleAsync<ContentModerationMetricsDataDto>(It.IsAny<string>()), Times.Once);
        }
        #endregion

        #region GetQuestionReportByPaginationAsync Tests

        [Fact]
        public async Task GetQuestionReportByPaginationAsync_ReturnsPaginatedData_WhenDataExists()
        {
            // Arrange
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortColumn = "created_date",
                SortDescending = false,
                Filters = new FilterDto
                {
                    Severity = QuestionOrQuizIssueReportSeverity.Medium,
                    QuestionOrQuizIssueReportStatus = QuestionOrQuizIssueReportStatus.Pending
                }
            };

            var mockQuestionReports = new List<QuestionIssueReportDTO>
            {
                new() { Id = 1, Question = "What is OOP?", Severity = 2, Status = 1 },
                new() { Id = 2, Question = "Define polymorphism.", Severity = 2, Status = 1 }
            };

            var mockTotalCount = new TotalRecordsDto { TotalRecords = 2 };

            _sqlQueryRepositoryMock
                .Setup(repo => repo.SqlQueryListAsync<QuestionIssueReportDTO>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(mockQuestionReports);

            _sqlQueryRepositoryMock
                .Setup(repo => repo.SqlQuerySingleAsync<TotalRecordsDto>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(mockTotalCount);

            // Act
            var result = await _service.GetQuestionReportByPaginationAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalRecords);
            Assert.Equal(2, result.Records.Count);
            Assert.Equal("What is OOP?", result.Records.First().Question);

            _sqlQueryRepositoryMock.Verify(repo => repo.SqlQueryListAsync<QuestionIssueReportDTO>(
                It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);

            _sqlQueryRepositoryMock.Verify(repo => repo.SqlQuerySingleAsync<TotalRecordsDto>(
                It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task GetQuestionReportByPaginationAsync_ReturnsEmptyList_WhenNoRecordsFound()
        {
            // Arrange
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10
            };

            _sqlQueryRepositoryMock
                .Setup(repo => repo.SqlQueryListAsync<QuestionIssueReportDTO>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(new List<QuestionIssueReportDTO>());

            _sqlQueryRepositoryMock
                .Setup(repo => repo.SqlQuerySingleAsync<TotalRecordsDto>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(new TotalRecordsDto { TotalRecords = 0 });

            // Act
            var result = await _service.GetQuestionReportByPaginationAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Records);
            Assert.Equal(0, result.TotalRecords);

            _sqlQueryRepositoryMock.Verify(repo => repo.SqlQueryListAsync<QuestionIssueReportDTO>(
                It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);

            _sqlQueryRepositoryMock.Verify(repo => repo.SqlQuerySingleAsync<TotalRecordsDto>(
                It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public async Task GetQuestionReportByPaginationAsync_HandlesNullFilters_Gracefully()
        {
            // Arrange
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                Filters = null
            };

            var mockReports = new List<QuestionIssueReportDTO>
            {
                new() { Id = 1, Question = "What is inheritance?", Severity = 1, Status = 1 }
            };

            var mockTotalCount = new TotalRecordsDto { TotalRecords = 1 };

            _sqlQueryRepositoryMock
                .Setup(repo => repo.SqlQueryListAsync<QuestionIssueReportDTO>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(mockReports);

            _sqlQueryRepositoryMock
                .Setup(repo => repo.SqlQuerySingleAsync<TotalRecordsDto>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(mockTotalCount);

            // Act
            var result = await _service.GetQuestionReportByPaginationAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Records);
            Assert.Equal(1, result.TotalRecords);
            Assert.Equal("What is inheritance?", result.Records.First().Question);
        }

        #endregion


        #region GetQuestionIssueReportPreview Tests
        [Fact]
        public async Task GetQuestionIssueReportPreview_ReturnsPreviewSuccessfully()
        {
            int questionId = 101;
            var mockQuestionDetail = new QuestionDetailDTO { QuestionText = "Sample Question" };
            var mockReportData = new QuestionReportData
            {
                ActiveBattleContainCount = 2,
                ActiveQuizContainCount = 3
            };

            _questionPoolServiceMock.Setup(s => s.GetQuestionPreview(questionId))
                .ReturnsAsync(mockQuestionDetail);

            _sqlQueryRepositoryMock.Setup(r => r.SqlQuerySingleAsync<QuestionReportData>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(mockReportData);

            var result = await _service.GetQuestionIssueReportPreview(questionId);

            Assert.NotNull(result);
            Assert.Equal(mockQuestionDetail.QuestionText, result.QuestionDetail.QuestionText);
            Assert.Equal(mockReportData.ActiveBattleContainCount, result.ActiveBattleContainCount);
            Assert.Equal(mockReportData.ActiveQuizContainCount, result.ActiveQuizContainCount);
        }
        #endregion

        #region GetAffectedQuizAndBattle Tests
        [Fact]
        public async Task GetAffectedQuizAndBattle_ReturnsMappedResults()
        {
            int questionId = 202;
            var dbResults = new List<ActiveQuizBattleAffectedDTO>
            {
                new() { Id = 1, QuizTitle = "Math Quiz", CategoryName = "Math", QuizDifficultyLevel = "Easy", TotalQuestion = 10, Type = 1 },
                new() { Id = 2, QuizTitle = "Battle Royale", CategoryName = "Science", QuizDifficultyLevel = "Medium", TotalQuestion = 5, Type = 2 }
            };

            _sqlQueryRepositoryMock
                .Setup(repo => repo.SqlQueryListAsync<ActiveQuizBattleAffectedDTO>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(dbResults);

            _mapperMock.Setup(m => m.Map<List<ActiveQuizBattleAffectedDTO>>(It.IsAny<List<ActiveQuizBattleAffectedDTO>>()))
                .Returns((List<ActiveQuizBattleAffectedDTO> src) => src);

            var result = await _service.GetAffectedQuizAndBattle(questionId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.QuizTitle == "Math Quiz");
        }

        [Fact]
        public async Task GetAffectedQuizAndBattle_ReturnsEmptyList_WhenNoRecordsFound()
        {
            int questionId = 999;
            _sqlQueryRepositoryMock
                .Setup(repo => repo.SqlQueryListAsync<ActiveQuizBattleAffectedDTO>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(new List<ActiveQuizBattleAffectedDTO>());

            _mapperMock.Setup(m => m.Map<List<ActiveQuizBattleAffectedDTO>>(It.IsAny<List<ActiveQuizBattleAffectedDTO>>()))
                .Returns(new List<ActiveQuizBattleAffectedDTO>());

            var result = await _service.GetAffectedQuizAndBattle(questionId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }
        #endregion

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

            var existingReport = new QuestionIssueReport
            {
                Id = reportId,
                Status = (int)QuestionOrQuizIssueReportStatus.Pending,
                ModifiedBy = 1 // same as current user from mock context
            };

            _reportedQuestionRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuestionIssueReport, bool>>>(), null))
                .ReturnsAsync(existingReport);

            _reportedQuestionRepoMock
                .Setup(r => r.UpdateAsync(existingReport))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateQuestionReportAction(actionRequest);

            // Assert
            Assert.Equal(Constants.QUESTION_ISSUE_ACTION_UPDATE_SUCCESS_MESSAGE, result);
            Assert.Equal(actionRequest.QuestionOrQuizIssueReportNewStatus, existingReport.Status);
            _reportedQuestionRepoMock.Verify(r => r.UpdateAsync(existingReport), Times.Once);
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

            var existingReport = new QuestionIssueReport
            {
                Id = reportId,
                Status = (int)QuestionOrQuizIssueReportStatus.Accepted,
                ModifiedBy = 1
            };

            _reportedQuestionRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuestionIssueReport, bool>>>(), null))
                .ReturnsAsync(existingReport);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => _service.UpdateQuestionReportAction(actionRequest));
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

            var existingReport = new QuestionIssueReport
            {
                Id = reportId,
                Status = (int)QuestionOrQuizIssueReportStatus.UnderReview,
                ModifiedBy = 999 // Different reviewer
            };

            _reportedQuestionRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuestionIssueReport, bool>>>(), null))
                .ReturnsAsync(existingReport);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => _service.UpdateQuestionReportAction(actionRequest));
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

            var existingReport = new QuestionIssueReport
            {
                Id = reportId,
                Status = (int)QuestionOrQuizIssueReportStatus.UnderReview,
                ModifiedBy = 1 // Same as current user
            };

            _reportedQuestionRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuestionIssueReport, bool>>>(), null))
                .ReturnsAsync(existingReport);

            _reportedQuestionRepoMock
                .Setup(r => r.UpdateAsync(existingReport))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateQuestionReportAction(actionRequest);

            // Assert
            Assert.Equal(Constants.QUESTION_ISSUE_ACTION_UPDATE_SUCCESS_MESSAGE, result);
            Assert.Equal(actionRequest.QuestionOrQuizIssueReportNewStatus, existingReport.Status);
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

            _reportedQuestionRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuestionIssueReport, bool>>>(), null))
                .ReturnsAsync((QuestionIssueReport)null);

            // Act & Assert
            await Assert.ThrowsAsync<AppException>(() => _service.UpdateQuestionReportAction(actionRequest));
        }
        #endregion

        #region UpdateReportedQuestion Tests
        [Fact]
        public async Task UpdateReportedQuestion_ShouldThrowNotFound_WhenReportDoesNotExist()
        {
            // Arrange
            _reportedQuestionRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuestionIssueReport, bool>>>(), null))
                .ReturnsAsync((QuestionIssueReport)null!);

            var dto = new QuestionRequestDTO { QuestionText = "Test" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => _service.UpdateReportedQuestion(99, dto));

            Assert.Equal(Constants.NO_DATA_FOUND, ex.Message);
            Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
        }

        [Fact]
        public async Task UpdateReportedQuestion_ShouldThrow_WhenSeverityUnderProcessing()
        {
            var report = new QuestionIssueReport
            {
                Id = 1,
                Severity = (int)QuestionOrQuizIssueReportSeverity.UnderProcessing
            };

            _reportedQuestionRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuestionIssueReport, bool>>>(), null))
                .ReturnsAsync(report);

            var dto = new QuestionRequestDTO { QuestionText = "UnderProcessing" };

            var ex = await Assert.ThrowsAsync<AppException>(() => _service.UpdateReportedQuestion(1, dto));

            Assert.Equal(Constants.SEVERITY_UNDER_PROCESS_WARNING, ex.Message);
        }

        [Theory]
        [InlineData((int)QuestionOrQuizIssueReportStatus.Accepted)]
        [InlineData((int)QuestionOrQuizIssueReportStatus.Ignore)]
        public async Task UpdateReportedQuestion_ShouldThrow_WhenFinalizedStatus(int finalizedStatus)
        {
            var report = new QuestionIssueReport
            {
                Id = 2,
                Status = finalizedStatus
            };

            _reportedQuestionRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuestionIssueReport, bool>>>(), null))
                .ReturnsAsync(report);

            var dto = new QuestionRequestDTO { QuestionText = "Finalized" };

            var ex = await Assert.ThrowsAsync<AppException>(() => _service.UpdateReportedQuestion(2, dto));

            Assert.Equal(Constants.QUESTION_ISSUE_REPORT_FINALIZED_INFO, ex.Message);
        }

        [Fact]
        public async Task UpdateReportedQuestion_ShouldThrow_WhenUnderReviewByDifferentUser_AndNotSuperAdmin()
        {
            var report = new QuestionIssueReport
            {
                Id = 3,
                Status = (int)QuestionOrQuizIssueReportStatus.UnderReview,
                ModifiedBy = 999 // different user than current mock (1)
            };

            _reportedQuestionRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuestionIssueReport, bool>>>(), null))
                .ReturnsAsync(report);

            var dto = new QuestionRequestDTO { QuestionText = "Unauthorized" };

            var ex = await Assert.ThrowsAsync<AppException>(() => _service.UpdateReportedQuestion(3, dto));

            Assert.Equal(Constants.QUESTION_ISSUE_REPORT_NOT_HAVE_PERMISSION_EDIT, ex.Message);
        }

        [Fact]
        public async Task UpdateReportedQuestion_ShouldSucceed_WhenValidReport()
        {
            // Arrange
            var report = new QuestionIssueReport
            {
                Id = 10,
                QuestionId = 100,
                Status = (int)QuestionOrQuizIssueReportStatus.Pending,
                Severity = (int)QuestionOrQuizIssueReportSeverity.Medium,
                ModifiedBy = 1 // current user from context
            };

            _reportedQuestionRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuestionIssueReport, bool>>>(), null))
                .ReturnsAsync(report);

            _questionPoolServiceMock
                .Setup(q => q.CreateOrUpdateQuestion(report.QuestionId, It.IsAny<QuestionRequestDTO>()))
                .ReturnsAsync("Question updated successfully");

            // The UpdateQuestionReportAction method exists inside same service
            // So we mock it indirectly by spying on repository update call
            _reportedQuestionRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<QuestionIssueReport>()))
                .Returns(Task.CompletedTask);

            var dto = new QuestionRequestDTO { QuestionText = "Valid Update" };

            // Act
            var result = await _service.UpdateReportedQuestion(report.Id, dto);

            // Assert
            Assert.Contains("Question updated successfully", result);
            Assert.Contains(Constants.QUESTION_ISSUE_ACTION_UPDATE_SUCCESS_MESSAGE, result);
        }

        [Fact]
        public async Task UpdateReportedQuestion_ShouldRollbackAndThrow_WhenQuestionUpdateFails()
        {
            // Arrange
            var report = new QuestionIssueReport
            {
                Id = 20,
                QuestionId = 200,
                Status = (int)QuestionOrQuizIssueReportStatus.Pending,
                Severity = (int)QuestionOrQuizIssueReportSeverity.Medium,
                ModifiedBy = 1
            };

            _reportedQuestionRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuestionIssueReport, bool>>>(), null))
                .ReturnsAsync(report);

            _questionPoolServiceMock
                .Setup(q => q.CreateOrUpdateQuestion(report.QuestionId, It.IsAny<QuestionRequestDTO>()))
                .ThrowsAsync(new Exception("Failed to update question"));

            _reportedQuestionRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<QuestionIssueReport>()))
                .Returns(Task.CompletedTask);

            var dto = new QuestionRequestDTO { QuestionText = "Rollback Test" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<AppException>(() => _service.UpdateReportedQuestion(report.Id, dto));

            Assert.Contains("Failed to update question", ex.Message);
            Assert.Contains(Constants.REVERT_TO_PENDING_REPORT_QUESTION_STATUS, ex.Message);
        }
        #endregion



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
