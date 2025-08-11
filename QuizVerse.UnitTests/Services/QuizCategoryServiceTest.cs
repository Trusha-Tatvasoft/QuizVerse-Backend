using Xunit;
using Moq;
using System.Linq.Expressions;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using QuizVerse.Infrastructure.Common;
using Npgsql;
using System.Security.Claims;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Common.Exceptions;

namespace QuizVerse.UnitTests.Services
{
    public class QuizCategoryServiceTest
    {
        private readonly Mock<IGenericRepository<QuizCategory>> _quizCategoryRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly QuizCategoryService _service;
        private readonly Mock<ISqlQueryRepository> _sqlQueryRepositoryMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
 
        public QuizCategoryServiceTest()
        {
            _quizCategoryRepoMock = new Mock<IGenericRepository<QuizCategory>>();
            _mapperMock = new Mock<IMapper>();
            _sqlQueryRepositoryMock = new Mock<ISqlQueryRepository>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.UserData, "1")], "mock"));
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            // Instantiate service with the correct accessor mock
            _service = new QuizCategoryService(
                _quizCategoryRepoMock.Object,
                _mapperMock.Object,
                _httpContextAccessorMock.Object,
                _sqlQueryRepositoryMock.Object
            );


        }
 
        [Fact]
        public async Task GetQuizCategories_ShouldReturnMappedDtosWithQuizCount()
        {
            // Arrange
            var quizCategories = new List<QuizCategory>
            {
                new QuizCategory { Id = 1, CategoryName = "Math", Description = "Math Desc", Quizzes = new List<Quiz> { new Quiz(), new Quiz() } },
                new QuizCategory { Id = 2, CategoryName = "Science", Description = "Sci Desc", Quizzes = new List<Quiz>() }
            }.AsQueryable();
 
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10
            };
 
            var pagedResult = new PageListResponse<QuizCategory>
            {
                Records = quizCategories.ToList(),
                TotalRecords = quizCategories.Count()
            };
 
            _quizCategoryRepoMock.Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<QuizCategory, object>>>()))
                .Returns(quizCategories);
 
            _quizCategoryRepoMock.Setup(r => r.PaginatedList<QuizCategory>(It.IsAny<IQueryable<QuizCategory>>(), request, null))
                .ReturnsAsync(pagedResult);
 
            _mapperMock.Setup(m => m.Map<List<QuizCategoryDTO>>(pagedResult.Records))
                .Returns(pagedResult.Records.Select(q => new QuizCategoryDTO
                {
                    Id = q.Id,
                    CategoryName = q.CategoryName,
                    Description = q.Description,
                    IsActive = q.Status,
                    CreatedDate = q.CreatedDate
                }).ToList());
 
            // Act
            var result = await _service.GetQuizCategories(request);
 
            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Records.Count);
            Assert.Equal(2, result.Records.First().QuizCount);
            Assert.Equal(0, result.Records.Last().QuizCount);
            Assert.Equal(2, result.TotalRecords);
        }
        [Fact]
        public async Task GetQuizCategories_WithSearch_ShouldFilterResultsCorrectly()
        {
            // Arrange
            var quizCategories = new List<QuizCategory>
            {
                new QuizCategory { Id = 1, CategoryName = "Math", Description = "Numbers", Quizzes = new List<Quiz>() },
                new QuizCategory { Id = 2, CategoryName = "Science", Description = "Experiments", Quizzes = new List<Quiz>() }
            }.AsQueryable();
 
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "math"
            };
 
            var filtered = quizCategories.Where(q =>
                q.CategoryName.ToLower().Contains("math") || q.Description.ToLower().Contains("math")).ToList();
 
            var pagedResult = new PageListResponse<QuizCategory>
            {
                Records = filtered,
                TotalRecords = filtered.Count
            };
 
            _quizCategoryRepoMock.Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<QuizCategory, object>>>()))
                .Returns(quizCategories);
 
            _quizCategoryRepoMock.Setup(r => r.PaginatedList<QuizCategory>(It.IsAny<IQueryable<QuizCategory>>(), request, null))
                .ReturnsAsync(pagedResult);
 
            _mapperMock.Setup(m => m.Map<List<QuizCategoryDTO>>(pagedResult.Records))
                .Returns(filtered.Select(q => new QuizCategoryDTO
                {
                    Id = q.Id,
                    CategoryName = q.CategoryName,
                    Description = q.Description
                }).ToList());
 
            // Act
            var result = await _service.GetQuizCategories(request);
 
            // Assert
            Assert.Single(result.Records);
            Assert.Equal("Math", result.Records[0].CategoryName);
        }
 
        [Fact]
        public async Task GetQuizCategories_WithInvalidPageNumber_ShouldThrowException()
        {
            // Arrange
            var quizCategories = new List<QuizCategory>
            {
                new QuizCategory { Id = 1, CategoryName = "Math", Description = "Desc", Quizzes = new List<Quiz>() }
            }.AsQueryable();
 
            var request = new PageListRequest
            {
                PageNumber = 5,
                PageSize = 1
            };
 
            _quizCategoryRepoMock.Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<QuizCategory, object>>>()))
                .Returns(quizCategories);
 
            // TotalRecords = 1 → MaxPage = 1 → request.PageNumber = 5 → should throw
            _quizCategoryRepoMock.Setup(r => r.PaginatedList<QuizCategory>(It.IsAny<IQueryable<QuizCategory>>(), request, null))
                .ReturnsAsync(new PageListResponse<QuizCategory>
                {
                    Records = new List<QuizCategory>(),
                    TotalRecords = 1
                });
 
            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.GetQuizCategories(request));
            Assert.Contains("PageNumber", ex.ParamName);
        }
 
        [Fact]
        public async Task GetQuizCategories_WithInvalidSortColumn_ShouldThrowException()
        {
            // Arrange
            var quizCategories = new List<QuizCategory>
            {
                new QuizCategory { Id = 1, CategoryName = "Math", Description = "Desc", Quizzes = new List<Quiz>() }
            }.AsQueryable();
 
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortColumn = "InvalidColumn"
            };
 
            _quizCategoryRepoMock.Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<QuizCategory, object>>>()))
                .Returns(quizCategories);
 
            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.GetQuizCategories(request));
            Assert.Equal(nameof(PageListRequest.SortColumn), ex.ParamName);
            Assert.Contains("InvalidColumn", ex.Message);
        }
 
        [Fact]
        public async Task GetQuizCategories_WithValidSortColumn_ShouldSortCorrectly()
        {
            // Arrange
            var quizCategories = new List<QuizCategory>
            {
                new QuizCategory { Id = 2, CategoryName = "Zoology", Description = "", Quizzes = new List<Quiz>() },
                new QuizCategory { Id = 1, CategoryName = "Algebra", Description = "", Quizzes = new List<Quiz>() }
            }.AsQueryable();
 
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SortColumn = "CategoryName",
                SortDescending = false
            };
 
            var pagedResult = new PageListResponse<QuizCategory>
            {
                Records = quizCategories.ToList(),
                TotalRecords = quizCategories.Count()
            };
 
            _quizCategoryRepoMock.Setup(r => r.GetQueryableInclude(It.IsAny<Expression<Func<QuizCategory, object>>>()))
                .Returns(quizCategories);
 
            _quizCategoryRepoMock.Setup(r => r.PaginatedList<QuizCategory>(It.IsAny<IQueryable<QuizCategory>>(), request, null))
                .ReturnsAsync(pagedResult);
 
            _mapperMock.Setup(m => m.Map<List<QuizCategoryDTO>>(pagedResult.Records))
                .Returns(pagedResult.Records.Select(q => new QuizCategoryDTO
                {
                    Id = q.Id,
                    CategoryName = q.CategoryName
                }).ToList());
 
            // Act
            var result = await _service.GetQuizCategories(request);
 
            // Assert
            Assert.Equal(2, result.Records.Count);
            Assert.Equal("Zoology", result.Records[0].CategoryName);
        }

        [Fact]
        public async Task CreateOrUpdateQuizCategory_ShouldReturnSuccess_WhenUserIdPresent()
        {

            QuizCategoryDTO dto = new()
            {
                Id = 1,
                CategoryName = "Science",
                Description = "General Science",
                Icon = "science-icon"
            };

            RawQuizCategoryResponseDto expectedResponse = new()
            {
                Success = true,
                Message = "Category created successfully"
            };

            _sqlQueryRepositoryMock
                .Setup(r => r.SqlQuerySingleAsync<RawQuizCategoryResponseDto>(
                    SqlConstants.FN_CREATE_OR_UPDATE_QUIZ_CATEGORY,
                    It.IsAny<NpgsqlParameter[]>()
                ))
                .ReturnsAsync(expectedResponse);

            (bool Success,string Message) = await _service.CreateOrUpdateQuizCategory(dto);

            Assert.True(Success);
            Assert.Equal("Category created successfully", Message);

            _sqlQueryRepositoryMock.Verify(r => r.SqlQuerySingleAsync<RawQuizCategoryResponseDto>(
                SqlConstants.FN_CREATE_OR_UPDATE_QUIZ_CATEGORY,
                It.Is<NpgsqlParameter[]>(p => p.Any(x => x.ParameterName == "@p_user_id" && (int)x.Value == 1))
            ), Times.Once);
        }


        [Fact]
        public async Task CreateOrUpdateQuizCategory_ShouldReturnFailure_WhenRepositoryReturnsFailure()
        {
            QuizCategoryDTO dto = new()
            {
                Id = null,
                CategoryName = "Math",
                Description = "",
                Icon = ""
            };

            RawQuizCategoryResponseDto expectedResponse = new()
            {
                Success = false,
                Message = "Category already exists"
            };

            _sqlQueryRepositoryMock
                .Setup(r => r.SqlQuerySingleAsync<RawQuizCategoryResponseDto>(
                    SqlConstants.FN_CREATE_OR_UPDATE_QUIZ_CATEGORY,
                    It.IsAny<NpgsqlParameter[]>()
                ))
                .ReturnsAsync(expectedResponse);

            (bool Success,string Message) = await _service.CreateOrUpdateQuizCategory(dto);

            Assert.False(Success);
            Assert.Equal("Category already exists", Message);
        }

        [Fact]
        public async Task CreateOrUpdateQuizCategory_ShouldThrowException_WhenUserIdNotFound()
        {
            QuizCategoryDTO dto = new()
            {
                Id = 1,
                CategoryName = "Science",
                Description = "General Science",
                Icon = "science-icon"
            };

            DefaultHttpContext httpContext = new DefaultHttpContext();
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            Exception ex = await Assert.ThrowsAsync<Exception>(() => _service.CreateOrUpdateQuizCategory(dto));

            Assert.Contains("User with ID", ex.Message);
        }

        [Fact]
        public async Task UpdateQuizCategoryByAction_ShouldDeleteSuccessfully_WhenNotDeleted()
        {
            QuizCategory category = new QuizCategory { Id = 1, Status = true, IsDeleted = false };
            _quizCategoryRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuizCategory, bool>>>(), null))
                .ReturnsAsync(category);

            _quizCategoryRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<QuizCategory>()))
                .Returns(Task.CompletedTask);

            QuizCategoryActionRequestDto request = new()
            {
                Id = 1,
                Action = QuizCategoryActionType.Delete
            };

            string result = await _service.UpdateQuizCategoryByAction(request);

            Assert.Equal(Constants.DELETE_SUCCESS, result);
            Assert.True(category.IsDeleted);
            _quizCategoryRepoMock.Verify(r => r.UpdateAsync(category), Times.AtLeastOnce);
        }

        [Fact]
        public async Task UpdateQuizCategoryByAction_ShouldThrow_WhenAlreadyDeleted()
        {
            QuizCategory category = new() { Id = 1, Status = true, IsDeleted = true };
            _quizCategoryRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuizCategory, bool>>>(), null))
                .ReturnsAsync(category);

            QuizCategoryActionRequestDto request = new()
            {
                Id = 1,
                Action = QuizCategoryActionType.Delete
            };

            AppException ex = await Assert.ThrowsAsync<AppException>(() => _service.UpdateQuizCategoryByAction(request));
            Assert.Contains(string.Format(Constants.USER_ALREADY_DELETED, 1), ex.Message);
        }

        [Fact]
        public async Task UpdateQuizCategoryByAction_ShouldThrow_WhenNewStatusIsNull()
        {
            QuizCategory category = new QuizCategory { Id = 1, Status = true, IsDeleted = false };
            _quizCategoryRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuizCategory, bool>>>(), null))
                .ReturnsAsync(category);

            QuizCategoryActionRequestDto request = new()
            {
                Id = 1,
                Action = QuizCategoryActionType.ChangeStatus,
                NewStatus = null
            };

            AppException ex = await Assert.ThrowsAsync<AppException>(() => _service.UpdateQuizCategoryByAction(request));
            Assert.Equal(Constants.STATUS_REQUIRED, ex.Message);
        }

        [Fact]
        public async Task UpdateQuizCategoryByAction_ShouldThrow_WhenStatusAlreadySet()
        {
            QuizCategory category = new(){ Id = 1, Status = true, IsDeleted = false };
            _quizCategoryRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuizCategory, bool>>>(), null))
                .ReturnsAsync(category);

            QuizCategoryActionRequestDto request = new()
            {
                Id = 1,
                Action = QuizCategoryActionType.ChangeStatus,
                NewStatus = (QuizCategoryStatus?)1 
            };

            AppException ex = await Assert.ThrowsAsync<AppException>(() => _service.UpdateQuizCategoryByAction(request));
            Assert.Contains(string.Format(Constants.QUIZ_CATEGORY_STATUS_ALREADY_SET, QuizCategoryStatus.Active), ex.Message);
        }

        [Fact]
        public async Task UpdateQuizCategoryByAction_ShouldChangeStatusFromTrueToFalse()
        {
            // Arrange
            QuizCategory category = new QuizCategory { Id = 1, Status = true, IsDeleted = false };
            _quizCategoryRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuizCategory, bool>>>(), null))
                .ReturnsAsync(category);

            _quizCategoryRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<QuizCategory>()))
                .Returns(Task.CompletedTask);

            QuizCategoryActionRequestDto request = new()
            {
                Id = 1,
                Action = QuizCategoryActionType.ChangeStatus,
                NewStatus = 0
            };

            // Act
            string result = await _service.UpdateQuizCategoryByAction(request);

            // Assert
            Assert.False(category.Status);
            Assert.Contains(category.Id.ToString(), result);
            _quizCategoryRepoMock.Verify(r => r.UpdateAsync(category), Times.AtLeastOnce);
        }

        [Fact]
        public async Task UpdateQuizCategoryByAction_ShouldChangeStatusFromFalseToTrue()
        {
            // Arrange
            QuizCategory category = new QuizCategory { Id = 1, Status = false, IsDeleted = false };
            _quizCategoryRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuizCategory, bool>>>(), null))
                .ReturnsAsync(category);

            _quizCategoryRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<QuizCategory>()))
                .Returns(Task.CompletedTask);

            QuizCategoryActionRequestDto request = new()
            {
                Id = 1,
                Action = QuizCategoryActionType.ChangeStatus,
                NewStatus = (QuizCategoryStatus?)1
            };

            string result = await _service.UpdateQuizCategoryByAction(request);

            Assert.True(category.Status);
            Assert.Contains(category.Id.ToString(), result);
            _quizCategoryRepoMock.Verify(r => r.UpdateAsync(category), Times.AtLeastOnce);
        }

        [Fact]
        public async Task UpdateQuizCategoryByAction_ShouldThrow_WhenInvalidActionType()
        {
            QuizCategory category = new() { Id = 1, Status = false, IsDeleted = false };
            _quizCategoryRepoMock
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<QuizCategory, bool>>>(), null))
                .ReturnsAsync(category);

            QuizCategoryActionRequestDto request = new()
            {
                Id = 1,
                Action = (QuizCategoryActionType)999
            };

            AppException ex = await Assert.ThrowsAsync<AppException>(() => _service.UpdateQuizCategoryByAction(request));
            Assert.Equal(Constants.INVALID_DATA_MESSAGE, ex.Message);
        }
    }
}