using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using System.Linq.Expressions;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class ContentModerationServiceTests
    {
        private readonly Mock<IGenericRepository<QuizIssueReport>> _repoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly ContentModerationService _service;

        public ContentModerationServiceTests()
        {
            _repoMock = new Mock<IGenericRepository<QuizIssueReport>>();
            _mapperMock = new Mock<IMapper>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            // Mock authenticated user (Admin)
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "1"),
                        new Claim(ClaimTypes.Role, "Admin")
                    },
                    "mock"))
            };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            _service = new ContentModerationService(
                _repoMock.Object,
                _httpContextAccessorMock.Object,
                _mapperMock.Object
            );
        }

        #region Dummy Data

        private static IQueryable<QuizIssueReport> GetDummyReports()
        {
            var reports = new List<QuizIssueReport>
            {
                new()
                {
                    Id = 1,
                    Reason = "Typo in question",
                    Severity = 2,
                    Quiz = new Quiz { Name = "C# Basics", CreatedByNavigation = new User { FullName = "Admin 1" } },
                    User = new User { FullName = "User A" }
                },
                new()
                {
                    Id = 2,
                    Reason = "Incorrect answer",
                    Severity = 3,
                    Quiz = new Quiz { Name = "ASP.NET Core", CreatedByNavigation = new User { FullName = "Admin 2" } },
                    User = new User { FullName = "User B" }
                },
                new()
                {
                    Id = 3,
                    Reason = "Broken image link",
                    Severity = 1,
                    Quiz = new Quiz { Name = "Entity Framework", CreatedByNavigation = new User { FullName = "Admin 3" } },
                    User = new User { FullName = "User C" }
                }
            };
            return new TestAsyncEnumerable<QuizIssueReport>(reports); ;
        }

        #endregion

        #region GetQuizReportByPaginationAsync Tests

        [Fact]
        public async Task GetQuizReportByPaginationAsync_ReturnsPagedMappedData_WhenDataExists()
        {
            // Arrange
            var dummyReports = GetDummyReports();
            var mappedList = dummyReports.Select(x => new QuizReportIssueResponseDTO
            {
                Id = x.Id,
                QuizTitle = x.Quiz.Name,
                Reporter = x.User.FullName,
                Severity = x.Severity
            }).ToList();

            _repoMock.Setup(r => r.GetQueryableInclude(
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>()))
                .Returns(dummyReports);

            _mapperMock.Setup(m => m.Map<List<QuizReportIssueResponseDTO>>(It.IsAny<List<QuizIssueReport>>()))
                .Returns(mappedList);

            var query = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 2,
                SortColumn = "quiz",
                SortDescending = false
            };

            // Act
            var result = await _service.GetQuizReportByPaginationAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(mappedList.Count, result.TotalRecords);
            Assert.Equal(2, result.Records.Count());
            Assert.Equal("C# Basics", result.Records.First().QuizTitle);
        }

        [Fact]
        public async Task GetQuizReportByPaginationAsync_ReturnsEmpty_WhenNoDataExists()
        {
            // Arrange
            var emptyReports = new TestAsyncEnumerable<QuizIssueReport>(new List<QuizIssueReport>());

            _repoMock.Setup(r => r.GetQueryableInclude(
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>()))
                .Returns(emptyReports);

            _mapperMock.Setup(m => m.Map<List<QuizReportIssueResponseDTO>>(It.IsAny<List<QuizIssueReport>>()))
                .Returns(new List<QuizReportIssueResponseDTO>());

            var query = new PageListRequest { PageNumber = 1, PageSize = 5 };

            // Act
            var result = await _service.GetQuizReportByPaginationAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Records);
            Assert.Equal(0, result.TotalRecords);
        }


        [Fact]
        public async Task GetQuizReportByPaginationAsync_SortsBySeverity_WhenRequested()
        {
            // Arrange
            var dummyReports = GetDummyReports();
            var mappedList = dummyReports
                .OrderBy(r => r.Severity)
                .Select(x => new QuizReportIssueResponseDTO
                {
                    Id = x.Id,
                    QuizTitle = x.Quiz.Name,
                    Reporter = x.User.FullName,
                    Severity = x.Severity
                })
                .ToList();

            _repoMock.Setup(r => r.GetQueryableInclude(
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>()))
                .Returns(dummyReports);

            _mapperMock.Setup(m => m.Map<List<QuizReportIssueResponseDTO>>(It.IsAny<List<QuizIssueReport>>()))
                .Returns(mappedList);

            var query = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 3,
                SortColumn = "severity",
                SortDescending = false
            };

            // Act
            var result = await _service.GetQuizReportByPaginationAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalRecords);
            Assert.Equal(1, result.Records.First().Severity); // Lowest severity first
        }

        [Fact]
        public async Task GetQuizReportByPaginationAsync_SortsDescending_WhenRequested()
        {
            // Arrange
            var dummyReports = GetDummyReports();
            var mappedList = dummyReports
                .OrderByDescending(r => r.Severity)
                .Select(x => new QuizReportIssueResponseDTO
                {
                    Id = x.Id,
                    QuizTitle = x.Quiz.Name,
                    Reporter = x.User.FullName,
                    Severity = x.Severity
                })
                .ToList();

            _repoMock.Setup(r => r.GetQueryableInclude(
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>()))
                .Returns(dummyReports);

            _mapperMock.Setup(m => m.Map<List<QuizReportIssueResponseDTO>>(It.IsAny<List<QuizIssueReport>>()))
                .Returns(mappedList);

            var query = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 3,
                SortColumn = "severity",
                SortDescending = true
            };

            // Act
            var result = await _service.GetQuizReportByPaginationAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalRecords);
            Assert.Equal(3, result.Records.First().Severity); // Highest first
        }

        #endregion


        [Fact]
        public async Task GetQuizReportByPaginationAsync_SortsByCreator_WhenRequested()
        {
            // Arrange
            var dummyReports = GetDummyReports();
            var mappedList = dummyReports
                .OrderBy(r => r.Quiz.CreatedByNavigation.FullName)
                .Select(x => new QuizReportIssueResponseDTO
                {
                    Id = x.Id,
                    QuizTitle = x.Quiz.Name,
                    Reporter = x.User.FullName,
                    Severity = x.Severity
                })
                .ToList();

            _repoMock.Setup(r => r.GetQueryableInclude(
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>()))
                .Returns(dummyReports);

            _mapperMock.Setup(m => m.Map<List<QuizReportIssueResponseDTO>>(It.IsAny<List<QuizIssueReport>>()))
                .Returns(mappedList);

            var query = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 3,
                SortColumn = "creator",
                SortDescending = false
            };

            // Act
            var result = await _service.GetQuizReportByPaginationAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalRecords);
            Assert.Equal("User A", result.Records.First().Reporter); // Sorted by creator’s name (ascending)
        }

        [Fact]
        public async Task GetQuizReportByPaginationAsync_SortsByReporter_WhenRequested()
        {
            // Arrange
            var dummyReports = GetDummyReports();
            var mappedList = dummyReports
                .OrderBy(r => r.User.FullName)
                .Select(x => new QuizReportIssueResponseDTO
                {
                    Id = x.Id,
                    QuizTitle = x.Quiz.Name,
                    Reporter = x.User.FullName,
                    Severity = x.Severity
                })
                .ToList();

            _repoMock.Setup(r => r.GetQueryableInclude(
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>()))
                .Returns(dummyReports);

            _mapperMock.Setup(m => m.Map<List<QuizReportIssueResponseDTO>>(It.IsAny<List<QuizIssueReport>>()))
                .Returns(mappedList);

            var query = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 3,
                SortColumn = "reporter",
                SortDescending = false
            };

            // Act
            var result = await _service.GetQuizReportByPaginationAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalRecords);
            Assert.Equal("User A", result.Records.First().Reporter); // Lowest alphabetical name first
        }

        [Fact]
        public async Task GetQuizReportByPaginationAsync_UsesDefaultSort_WhenInvalidColumnProvided()
        {
            // Arrange
            var dummyReports = GetDummyReports();
            var mappedList = dummyReports
                .OrderBy(r => r.Id)
                .Select(x => new QuizReportIssueResponseDTO
                {
                    Id = x.Id,
                    QuizTitle = x.Quiz.Name,
                    Reporter = x.User.FullName,
                    Severity = x.Severity
                })
                .ToList();

            _repoMock.Setup(r => r.GetQueryableInclude(
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>(),
                    It.IsAny<Expression<Func<QuizIssueReport, object>>>()))
                .Returns(dummyReports);

            _mapperMock.Setup(m => m.Map<List<QuizReportIssueResponseDTO>>(It.IsAny<List<QuizIssueReport>>()))
                .Returns(mappedList);

            var query = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 3,
                SortColumn = "unknown_column", // triggers default case
                SortDescending = false
            };

            // Act
            var result = await _service.GetQuizReportByPaginationAsync(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalRecords);
            Assert.Equal(1, result.Records.First().Id); // Sorted by Id by default
        }
    }
}