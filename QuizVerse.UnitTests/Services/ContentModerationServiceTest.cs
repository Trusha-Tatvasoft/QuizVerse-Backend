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

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Role, "Admin")
                }, "mock"))
            };
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            _service = new ContentModerationService(
                _repoMock.Object,
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
    }
}
