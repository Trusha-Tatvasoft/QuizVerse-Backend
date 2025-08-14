using AutoMapper;
using Moq;
using Npgsql;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class QuizManagementServiceTests
    {
        private readonly Mock<IGenericRepository<Quiz>> _mockQuizRepo;
        private readonly Mock<ISqlQueryRepository> _mockSqlRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly QuizManagementService _service;

        public QuizManagementServiceTests()
        {
            _mockQuizRepo = new Mock<IGenericRepository<Quiz>>();
            _mockSqlRepo = new Mock<ISqlQueryRepository>();
            _mockMapper = new Mock<IMapper>();

            _service = new QuizManagementService(
                _mockQuizRepo.Object,
                _mockMapper.Object,
                _mockSqlRepo.Object
            );

        }
        #region Get Card Data
        [Fact]
        public async Task GetQuizCardData_ReturnsExpectedCounts_FromSqlRepository()
        {
            // Arrange - mock the SQL repository result
            var expectedDto = new QuizManagementPageDataDto
            {
                TotalQuiz = 2,
                ActiveQuiz = 1,
                TotalParticipants = 2,
                TotalQuestions = 3
            };

            _mockSqlRepo
                .Setup(r => r.SqlQuerySingleAsync<QuizManagementPageDataDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()
                ))
                .ReturnsAsync(expectedDto);

            // Act
            var result = await _service.GetQuizCardData();

            // Assert
            Assert.Equal(expectedDto.TotalQuiz, result.TotalQuiz);
            Assert.Equal(expectedDto.ActiveQuiz, result.ActiveQuiz);
            Assert.Equal(expectedDto.TotalParticipants, result.TotalParticipants);
            Assert.Equal(expectedDto.TotalQuestions, result.TotalQuestions);

            // Verify that the repository was called with the expected parameter
            _mockSqlRepo.Verify(r => r.SqlQuerySingleAsync<QuizManagementPageDataDto>(
                It.IsAny<string>(),
                It.Is<NpgsqlParameter[]>(p =>
                    p.Length == 1 &&
                    p[0].ParameterName == "p_active_status" &&
                    Convert.ToInt32(p[0].Value) == (int)QuizStatus.Active
                )
            ), Times.Once);
        }

        [Fact]
        public async Task GetQuizCardData_ReturnsEmptyDto_WhenSqlRepoReturnsNull()
        {
            // Arrange - mock repository to return null
            _mockSqlRepo
                .Setup(r => r.SqlQuerySingleAsync<QuizManagementPageDataDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()
                ))
                .ReturnsAsync((QuizManagementPageDataDto?)null!);

            // Act
            var result = await _service.GetQuizCardData();

            // Assert - should return a non-null empty DTO
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalQuiz);
            Assert.Equal(0, result.ActiveQuiz);
            Assert.Equal(0, result.TotalParticipants);
            Assert.Equal(0, result.TotalQuestions);
        }
        #endregion

        #region Get Quiz List
        [Fact]
        public async Task GetQuizzesByPagination_ReturnsMappedResults()
        {
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "math",
                SortColumn = "TotalQuestion",
                SortDescending = true,
                Filters = new FilterDto
                {
                    QuizStatus = QuizStatus.Active,
                    QuizCategoryId = 2,
                    QuizDifficultyId = 3
                }
            };

            var dbQuizzes = new List<QuizListDto>
            {
                new() { Id = 1, QuizTitle = "Math Quiz 1" },
                new() { Id = 2, QuizTitle = "Math Quiz 2" }
            };

            _mockSqlRepo
                .Setup(r => r.SqlQueryListAsync<QuizListDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(dbQuizzes);

            _mockSqlRepo
                .Setup(r => r.SqlQuerySingleAsync<TotalRecordsDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(new TotalRecordsDto { TotalRecords = 100 });

            _mockMapper
                .Setup(m => m.Map<List<QuizListDto>>(dbQuizzes))
                .Returns(dbQuizzes);

            // Act
            var result = await _service.GetQuizzesByPagination(request);

            // Assert
            Assert.Equal(100, result.TotalRecords);
            Assert.Equal(2, result.Records.Count);
            Assert.Equal("Math Quiz 1", result.Records[0].QuizTitle);
        }

        [Fact]
        public async Task GetQuizzesByPagination_EmptyResults_ReturnsZeroTotal()
        {
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 5
            };

            _mockSqlRepo
                .Setup(r => r.SqlQueryListAsync<QuizListDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(new List<QuizListDto>());

            _mockSqlRepo
                .Setup(r => r.SqlQuerySingleAsync<TotalRecordsDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()))
                .ReturnsAsync(new TotalRecordsDto { TotalRecords = 0 });

            _mockMapper
                .Setup(m => m.Map<List<QuizListDto>>(It.IsAny<List<QuizListDto>>()))
                .Returns(new List<QuizListDto>());

            // Act
            var result = await _service.GetQuizzesByPagination(request);

            // Assert
            Assert.Empty(result.Records);
            Assert.Equal(0, result.TotalRecords);
        }

        [Fact]
        public async Task GetQuizzesByPagination_Parameters_CorrectlyMapped()
        {
            var request = new PageListRequest
            {
                PageNumber = 3,
                PageSize = 20,
                SearchTerm = "history",
                SortColumn = "QuizTitle",
                SortDescending = true,
                Filters = new FilterDto
                {
                    QuizStatus = QuizStatus.Active,
                    QuizCategoryId = 2,
                    QuizDifficultyId = 5
                }
            };

            _mockSqlRepo.Setup(r =>
                r.SqlQueryListAsync<QuizListDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()
                ))
                .ReturnsAsync(new List<QuizListDto>());

            _mockSqlRepo.Setup(r =>
                r.SqlQuerySingleAsync<TotalRecordsDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()
                ))
                .ReturnsAsync(new TotalRecordsDto { TotalRecords = 0 });

            // Act
            await _service.GetQuizzesByPagination(request);

            // Assert all parameters
            _mockSqlRepo.Verify(r =>
                r.SqlQueryListAsync<QuizListDto>(
                    It.IsAny<string>(),
                    It.Is<NpgsqlParameter[]>(p =>
                        Convert.ToInt32(p.First(x => x.ParameterName == "p_page_number").Value) == 3 &&
                        Convert.ToInt32(p.First(x => x.ParameterName == "p_page_size").Value) == 20 &&
                        Convert.ToString(p.First(x => x.ParameterName == "p_search_term").Value) == "history" &&
                        Convert.ToString(p.First(x => x.ParameterName == "p_sort_column").Value) == "QuizTitle" &&
                        Convert.ToBoolean(p.First(x => x.ParameterName == "p_sort_descending").Value) == true &&
                        Convert.ToInt32(p.First(x => x.ParameterName == "p_quiz_status").Value) == (int)QuizStatus.Active &&
                        Convert.ToInt32(p.First(x => x.ParameterName == "p_category_id").Value) == 2 &&
                        Convert.ToInt32(p.First(x => x.ParameterName == "p_difficulty_id").Value) == 5
                    )
                ), Times.Once);

        }

        [Fact]
        public async Task GetQuizzesByPagination_OptionalParametersNull_UsesDBNull()
        {
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = null,
                SortColumn = null,
                SortDescending = false,
                Filters = null
            };

            _mockSqlRepo.Setup(r =>
                r.SqlQueryListAsync<QuizListDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()
                ))
                .ReturnsAsync(new List<QuizListDto>());

            _mockSqlRepo.Setup(r =>
                r.SqlQuerySingleAsync<TotalRecordsDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()
                ))
                .ReturnsAsync(new TotalRecordsDto { TotalRecords = 0 });

            // Act
            await _service.GetQuizzesByPagination(request);

            // Assert DBNull.Value for null parameters
            _mockSqlRepo.Verify(r =>
                r.SqlQueryListAsync<QuizListDto>(
                    It.IsAny<string>(),
                    It.Is<NpgsqlParameter[]>(p =>
                        p.First(x => x.ParameterName == "p_search_term").Value == DBNull.Value &&
                        p.First(x => x.ParameterName == "p_sort_column").Value == DBNull.Value &&
                        p.First(x => x.ParameterName == "p_quiz_status").Value == DBNull.Value &&
                        p.First(x => x.ParameterName == "p_category_id").Value == DBNull.Value &&
                        p.First(x => x.ParameterName == "p_difficulty_id").Value == DBNull.Value
                    )
                ), Times.Once);
        }

        [Fact]
        public async Task GetQuizzesByPagination_EmptyFilters_DefaultToDBNull()
        {
            var request = new PageListRequest
            {
                PageNumber = 1,
                PageSize = 10,
                Filters = new FilterDto() // all properties null / default
            };

            _mockSqlRepo.Setup(r =>
                r.SqlQueryListAsync<QuizListDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()
                ))
                .ReturnsAsync(new List<QuizListDto>());

            _mockSqlRepo.Setup(r =>
                r.SqlQuerySingleAsync<TotalRecordsDto>(
                    It.IsAny<string>(),
                    It.IsAny<NpgsqlParameter[]>()
                ))
                .ReturnsAsync(new TotalRecordsDto { TotalRecords = 0 });

            // Act
            await _service.GetQuizzesByPagination(request);

            _mockSqlRepo.Verify(r =>
                r.SqlQueryListAsync<QuizListDto>(
                    It.IsAny<string>(),
                    It.Is<NpgsqlParameter[]>(p =>
                        p.First(x => x.ParameterName == "p_quiz_status").Value == DBNull.Value &&
                        p.First(x => x.ParameterName == "p_category_id").Value == DBNull.Value &&
                        p.First(x => x.ParameterName == "p_difficulty_id").Value == DBNull.Value
                    )
                ), Times.Once);
        }


        #endregion
    }
}
