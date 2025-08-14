using AutoMapper;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class DropDownDataServiceTest
    {
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IMemoryCacheService> _cacheServiceMock;
        private readonly Mock<IGenericRepository<QuizCategory>> _quizCategoryRepoMock;
        private readonly Mock<IGenericRepository<QuizDifficulty>> _quizDifficultyRepoMock;
        private readonly Mock<IGenericRepository<QuizTag>> _quizTagRepoMock;
        private readonly Mock<IGenericRepository<QuestionDifficulty>> _questionDifficultyRepoMock;
        private readonly Mock<IGenericRepository<QuestionType>> _questionTypeRepoMock;

        private readonly DropDownDataService _service;

        public DropDownDataServiceTest()
        {
            _mapperMock = new Mock<IMapper>();
            _cacheServiceMock = new Mock<IMemoryCacheService>();
            _quizCategoryRepoMock = new Mock<IGenericRepository<QuizCategory>>();
            _quizDifficultyRepoMock = new Mock<IGenericRepository<QuizDifficulty>>();
            _quizTagRepoMock = new Mock<IGenericRepository<QuizTag>>();
            _questionDifficultyRepoMock = new Mock<IGenericRepository<QuestionDifficulty>>();
            _questionTypeRepoMock = new Mock<IGenericRepository<QuestionType>>();

            _service = new DropDownDataService(
                _mapperMock.Object,
                _cacheServiceMock.Object,
                _quizCategoryRepoMock.Object,
                _quizDifficultyRepoMock.Object,
                _quizTagRepoMock.Object,
                _questionDifficultyRepoMock.Object,
                _questionTypeRepoMock.Object);

            // Common mock setup
            _cacheServiceMock.Setup(c => c.GetOrSet(It.IsAny<string>(), It.IsAny<Func<List<CommonListDropDownDto>>>()))
                           .Returns((string key, Func<List<CommonListDropDownDto>> factory) => factory());
        }

        [Theory]
        [InlineData(DropDownType.QuizCategory)]
        [InlineData(DropDownType.QuizDifficulty)]
        [InlineData(DropDownType.QuizTag)]
        [InlineData(DropDownType.QuestionDifficulty)]
        [InlineData(DropDownType.QuestionType)]
        public void GetDropDownListData_ReturnsExpectedList(DropDownType type)
        {
            // Arrange
            var testData = new List<object>
            {
                new QuizCategory { Id = 1, CategoryName = "Test", IsDeleted = false },
                new QuizCategory { Id = 2, CategoryName = "Test 2", IsDeleted = false }
            }.AsQueryable();

            SetupRepositoryForType(type, testData);

            var expectedDtoList = new List<CommonListDropDownDto>
            {
                new() { Id = 1, Name = "Test" },
                new() { Id = 2, Name = "Test 2" }
            };

            _mapperMock.Setup(m => m.ProjectTo<CommonListDropDownDto>(
                It.IsAny<IQueryable<object>>(), It.IsAny<object[]>()))
                .Returns(expectedDtoList.AsQueryable());

            // Act
            var result = _service.GetDropDownListData(type);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(expectedDtoList[0].Id, result[0].Id);
            Assert.Equal(expectedDtoList[0].Name, result[0].Name);
        }

        [Fact]
        public void GetDropDownListData_AppliesIsDeletedFilter()
        {
            // Arrange
            var testData = new List<QuizCategory>
            {
                new() { Id = 1, CategoryName = "Active", IsDeleted = false },
                new() { Id = 2, CategoryName = "Deleted", IsDeleted = true }
            }.AsQueryable();

            _quizCategoryRepoMock.Setup(r => r.GetQueryableInclude()).Returns(testData);

            var expectedDtoList = new List<CommonListDropDownDto>
            {
                new() { Id = 1, Name = "Active" }
            };

            _mapperMock.Setup(m => m.ProjectTo<CommonListDropDownDto>(
                It.Is<IQueryable<object>>(q => q.Count() == 1), It.IsAny<object[]>()))
                .Returns(expectedDtoList.AsQueryable());

            // Act
            var result = _service.GetDropDownListData(DropDownType.QuizCategory);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public void GetDropDownListData_OrdersById()
        {
            // Arrange
            var testData = new List<QuizCategory>
            {
                new() { Id = 3, CategoryName = "Third", IsDeleted = false },
                new() { Id = 1, CategoryName = "First", IsDeleted = false },
                new() { Id = 2, CategoryName = "Second", IsDeleted = false }
            }.AsQueryable();

            _quizCategoryRepoMock.Setup(r => r.GetQueryableInclude()).Returns(testData);

            var expectedDtoList = new List<CommonListDropDownDto>
            {
                new() { Id = 1, Name = "First" },
                new() { Id = 2, Name = "Second" },
                new() { Id = 3, Name = "Third" }
            };

            _mapperMock.Setup(m => m.ProjectTo<CommonListDropDownDto>(
                It.Is<IQueryable<object>>(q => q.First().GetType().GetProperty("Id").GetValue(q.First()).Equals(1)), It.IsAny<object[]>()))
                .Returns(expectedDtoList.AsQueryable());

            // Act
            var result = _service.GetDropDownListData(DropDownType.QuizCategory);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[0].Id);
            Assert.Equal(2, result[1].Id);
            Assert.Equal(3, result[2].Id);
        }

        [Fact]
        public void GetDropDownListData_ThrowsForInvalidType()
        {
            // Arrange
            var invalidType = (DropDownType)999;

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => _service.GetDropDownListData(invalidType));
            Assert.Equal("dropDownType", ex.ParamName);
            Assert.Contains("999", ex.Message);
        }

        [Fact]
        public void ClearCache_CallsCacheClearWithCorrectKey()
        {
            // Arrange
            var type = DropDownType.QuizCategory;

            // Act
            _service.ClearCache(type);

            // Assert
            _cacheServiceMock.Verify(c => c.Clear("QuizCategory"), Times.Once);
        }

        private void SetupRepositoryForType(DropDownType type, IQueryable<object> data)
        {
            switch (type)
            {
                case DropDownType.QuizCategory:
                    _quizCategoryRepoMock.Setup(r => r.GetQueryableInclude()).Returns(data.Cast<QuizCategory>());
                    break;
                case DropDownType.QuizDifficulty:
                    _quizDifficultyRepoMock.Setup(r => r.GetQueryableInclude()).Returns(data.Cast<QuizDifficulty>());
                    break;
                case DropDownType.QuizTag:
                    _quizTagRepoMock.Setup(r => r.GetQueryableInclude()).Returns(data.Cast<QuizTag>());
                    break;
                case DropDownType.QuestionDifficulty:
                    _questionDifficultyRepoMock.Setup(r => r.GetQueryableInclude()).Returns(data.Cast<QuestionDifficulty>());
                    break;
                case DropDownType.QuestionType:
                    _questionTypeRepoMock.Setup(r => r.GetQueryableInclude()).Returns(data.Cast<QuestionType>());
                    break;
            }
        }
    }
}