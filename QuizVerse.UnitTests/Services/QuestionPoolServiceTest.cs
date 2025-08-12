using Xunit;
using Moq;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Interface;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuizVerse.UnitTests.Services
{
    public class QuestionPoolServiceTest
    {
        private readonly Mock<ISqlQueryRepository> _mockSqlQueryRepo;
        private readonly QuestionPoolService _service;

        public QuestionPoolServiceTest()
        {
            _mockSqlQueryRepo = new Mock<ISqlQueryRepository>();
            _service = new QuestionPoolService(_mockSqlQueryRepo.Object);
        }

        [Fact]
        public async Task GetQuestionPoolListAsync_ReturnsPaginatedList()
        {
            // Arrange
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "sample",
                SortColumn = "queText",
                SortDescending = false,
                Filters = new FilterDto
                {
                    QuizCategoryId = 1,
                    QuestionDifficultyId = 2,
                    QuestionTypeId = 3
                }
            };

            var questionPoolList = new List<QuestionPoolListDto>
            {
                new() { Id = 1, QueText = "Sample Question 1" },
                new() { Id = 2, QueText = "Sample Question 2" }
            };

            var totalRecords = new TotalRecordsDto { TotalRecords = 3 };

            _mockSqlQueryRepo
                .Setup(repo => repo.SqlQueryListAsync<QuestionPoolListDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(questionPoolList);

            _mockSqlQueryRepo
                .Setup(repo => repo.SqlQuerySingleAsync<TotalRecordsDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(totalRecords);

            // Act
            var result = await _service.GetQuestionPoolListAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalRecords);
            Assert.Equal(2, result.Records.Count);
        }

        [Fact]
        public async Task GetQuestionPoolListAsync_WithEmptyResults_ReturnsEmptyList()
        {
            // Arrange
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = null,
                SortColumn = null,
                SortDescending = false,
                Filters = null
            };

            var totalRecords = new TotalRecordsDto { TotalRecords = 0 };

            _mockSqlQueryRepo
                .Setup(repo => repo.SqlQueryListAsync<QuestionPoolListDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(new List<QuestionPoolListDto>());

            _mockSqlQueryRepo
                .Setup(repo => repo.SqlQuerySingleAsync<TotalRecordsDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(totalRecords);

            // Act
            var result = await _service.GetQuestionPoolListAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalRecords);
            Assert.Empty(result.Records);
        }

        [Fact]
        public async Task GetQuestionPoolListAsync_NullFilters_DoesNotThrow()
        {
            // Arrange
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 5,
                Filters = null
            };

            var totalRecords = new TotalRecordsDto { TotalRecords = 0 };

            _mockSqlQueryRepo
                .Setup(repo => repo.SqlQueryListAsync<QuestionPoolListDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(new List<QuestionPoolListDto>());

            _mockSqlQueryRepo
                .Setup(repo => repo.SqlQuerySingleAsync<TotalRecordsDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(totalRecords);

            // Act
            var result = await _service.GetQuestionPoolListAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Records);
            Assert.Equal(0, result.TotalRecords);
        }
    }
}
