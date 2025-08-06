using Xunit;
using Moq;
using System.Linq.Expressions;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Interface;
using AutoMapper;

namespace QuizVerse.UnitTests.Services
{
    public class QuizCategoryServiceTest
    {
        private readonly Mock<IGenericRepository<QuizCategory>> _quizCategoryRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly QuizCategoryService _service;

        public QuizCategoryServiceTest()
        {
            _quizCategoryRepoMock = new Mock<IGenericRepository<QuizCategory>>();
            _mapperMock = new Mock<IMapper>();
            _service = new QuizCategoryService(_quizCategoryRepoMock.Object, _mapperMock.Object);
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
        public void GetAllQuizCategories_ShouldReturnMappedDropdownList()
        {
            // Arrange
            var quizCategories = new List<QuizCategory>
            {
                new QuizCategory { Id = 1, CategoryName = "Math", IsDeleted = false },
                new QuizCategory { Id = 2, CategoryName = "Science", IsDeleted = false }
            }.AsQueryable();

            var expectedDtos = new List<CommonListDropDownDto>
            {
                new CommonListDropDownDto { Id = 1, Name = "Math" },
                new CommonListDropDownDto { Id = 2, Name = "Science" }
            };

            _quizCategoryRepoMock.Setup(r => r.GetQueryableInclude()).Returns(quizCategories);

            _mapperMock
                .Setup(m => m.ProjectTo<CommonListDropDownDto>(
                    It.IsAny<IQueryable<QuizCategory>>(),
                    It.IsAny<object>()))
                .Returns(expectedDtos.AsQueryable());


            // Act
            var result = _service.GetAllQuizCategories();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Math", result[0].Name);
            Assert.Equal("Science", result[1].Name);
        }

        [Fact]
        public void GetAllQuizCategories_WhenNoData_ShouldReturnEmptyList()
        {
            // Arrange
            var emptyCategories = new List<QuizCategory>().AsQueryable();
            var emptyDtos = new List<CommonListDropDownDto>().AsQueryable();

            _quizCategoryRepoMock.Setup(r => r.GetQueryableInclude()).Returns(emptyCategories);

            _mapperMock.Setup(m => m.ProjectTo<CommonListDropDownDto>(It.IsAny<IQueryable<QuizCategory>>(), null))
                .Returns(emptyDtos);

            // Act
            var result = _service.GetAllQuizCategories();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetAllQuizCategories_ShouldExcludeDeletedCategories()
        {
            // Arrange
            var quizCategories = new List<QuizCategory>
            {
                new QuizCategory { Id = 1, CategoryName = "Math", IsDeleted = false },
                new QuizCategory { Id = 2, CategoryName = "DeletedCat", IsDeleted = true }
            }.AsQueryable();

            var filtered = quizCategories.Where(q => !q.IsDeleted).AsQueryable();

            var expectedDtos = new List<CommonListDropDownDto>
            {
                new CommonListDropDownDto { Id = 1, Name = "Math" }
            }.AsQueryable();

            _quizCategoryRepoMock.Setup(r => r.GetQueryableInclude()).Returns(quizCategories);

            _mapperMock.Setup(m => m.ProjectTo<CommonListDropDownDto>(
                It.Is<IQueryable<QuizCategory>>(q => q.All(cat => !cat.IsDeleted)), It.IsAny<object>()))
                .Returns(expectedDtos);

            // Act
            var result = _service.GetAllQuizCategories();

            // Assert
            Assert.Single(result);
            Assert.Equal("Math", result.First().Name);
        }
    }
}
