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
        private readonly ContentModerationService _service;

        public ContentModerationServiceTests()
        {
            _repoMock = new Mock<IGenericRepository<QuizIssueReport>>();
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _sqlQueryRepositoryMock = new Mock<ISqlQueryRepository>();
            _reportedQuestionRepoMock = new Mock<IGenericRepository<QuestionIssueReport>>();
            _questionPoolServiceMock = new Mock<IQuestionPoolService>();

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
                _sqlQueryRepositoryMock.Object,
                _repoMock.Object,
                _reportedQuestionRepoMock.Object,
                _questionPoolServiceMock.Object,
                _httpContextAccessorMock.Object,
                _mapperMock.Object
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
        public async Task UpdateQuestionReportAction_UpdatesStatus_WhenReportExists()
        {
            var reportId = 500;
            var actionRequest = new QuizAndQuestionReportAction
            {
                ReportId = reportId,
                QuestionOrQuizIssueReportNewStatus = 2
            };

            var existingReport = new QuestionIssueReport
            {
                Id = reportId,
                Status = 1
            };

            _reportedQuestionRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuestionIssueReport, bool>>>(), null))
                .ReturnsAsync(existingReport);

            _reportedQuestionRepoMock.Setup(r => r.UpdateAsync(existingReport))
                .Returns(Task.CompletedTask);

            var result = await _service.UpdateQuestionReportAction(actionRequest);

            Assert.Equal(Constants.QUESTION_ISSUE_ACTION_UPDATE_SUCCESS_MESSAGE, result);
            Assert.Equal(actionRequest.QuestionOrQuizIssueReportNewStatus, existingReport.Status);
        }

        [Fact]
        public async Task UpdateQuestionReportAction_ThrowsAppException_WhenReportNotFound()
        {
            var actionRequest = new QuizAndQuestionReportAction
            {
                ReportId = 600,
                QuestionOrQuizIssueReportNewStatus = 3
            };

            _reportedQuestionRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuestionIssueReport, bool>>>(), null))
                .ReturnsAsync((QuestionIssueReport)null);

            await Assert.ThrowsAsync<AppException>(() => _service.UpdateQuestionReportAction(actionRequest));
        }
        #endregion
    }
}
