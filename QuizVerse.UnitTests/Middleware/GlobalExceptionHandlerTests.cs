using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.WebAPI.Middlewares;
using FluentAssertions;
using Xunit;

namespace QuizVerse.UnitTests.Middleware
{
    public class GlobalExceptionHandlerTests
    {
        private readonly Mock<ILogger<GlobalExceptionHandler>> _mockLogger;
        private readonly GlobalExceptionHandler _handler;

        public GlobalExceptionHandlerTests()
        {
            _mockLogger = new Mock<ILogger<GlobalExceptionHandler>>();
            _handler = new GlobalExceptionHandler(_mockLogger.Object);
        }

        // Tests known exceptions return correct status and message in ApiResponse
        [Theory]
        [InlineData(typeof(UnauthorizedAccessException), "Unauthorized access", StatusCodes.Status401Unauthorized)]
        [InlineData(typeof(NotImplementedException), "This feature is not implemented yet", StatusCodes.Status501NotImplemented)]
        [InlineData(typeof(KeyNotFoundException), "Resource not found", StatusCodes.Status404NotFound)]
        [InlineData(typeof(ArgumentNullException), "Missing required argument", StatusCodes.Status400BadRequest)]
        [InlineData(typeof(ArgumentException), "Invalid argument", StatusCodes.Status400BadRequest)]
        [InlineData(typeof(InvalidOperationException), "Invalid operation", StatusCodes.Status409Conflict)]
        [InlineData(typeof(TimeoutException), "Request timed out", StatusCodes.Status408RequestTimeout)]
        [InlineData(typeof(FileNotFoundException), "File not found", StatusCodes.Status404NotFound)]
        [InlineData(typeof(IOException), "I/O error", StatusCodes.Status500InternalServerError)]
        public async Task TryHandleAsync_ShouldReturnExpectedApiResponse_ForKnownExceptions(Type exceptionType, string expectedMessageStart, int expectedStatusCode)
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            Exception exception;
            if (exceptionType == typeof(ArgumentNullException))
            {
                exception = new ArgumentNullException("paramName");
            }
            else if (exceptionType == typeof(ArgumentException))
            {
                exception = new ArgumentException("Some argument is invalid");
            }
            else if (exceptionType == typeof(InvalidOperationException))
            {
                exception = new InvalidOperationException("Invalid operation attempted");
            }
            else if (exceptionType == typeof(FileNotFoundException))
            {
                exception = new FileNotFoundException("Test file missing", "testfile.txt");
            }
            else if (exceptionType == typeof(IOException))
            {
                exception = new IOException("Disk read error");
            }
            else
            {
                exception = (Exception)Activator.CreateInstance(exceptionType, "Generated for test")!;
            }

            var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);
            var response = await GetApiResponseFromContext(context);

            result.Should().BeTrue();
            context.Response.StatusCode.Should().Be(expectedStatusCode);
            response.Should().NotBeNull();
            response!.Result.Should().BeFalse();
            response.Message.Should().StartWith(expectedMessageStart);
            response.Data.Should().BeNull();

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("An error occurred")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ), Times.Once);
        }

        // Tests the query string is included whenn present in the URL
        [Fact]
        public async Task TryHandleAsync_ShouldIncludeQueryStringInLog_WhenQueryStringIsPresent()
        {
            var context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString("?test=123");
            context.Response.Body = new MemoryStream();

            var exception = new Exception("Test exception with query string");

            var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

            result.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("?test=123")),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ), Times.Once);
        }

        // Tests custom AppException returns its status code and message
        [Fact]
        public async Task TryHandleAsync_ShouldReturnExpectedResponse_ForAppException()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var exception = new AppException("Custom app error", 418);

            var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);
            var response = await GetApiResponseFromContext(context);

            result.Should().BeTrue();
            context.Response.StatusCode.Should().Be(418);
            response!.Message.Should().Be("Custom app error");
        }

        // Tests unknown exceptions return generic 500 error response
        [Fact]
        public async Task TryHandleAsync_ShouldReturnExpectedResponse_ForUnknownException()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var exception = new Exception("Some unknown error");

            var result = await _handler.TryHandleAsync(context, exception, CancellationToken.None);
            var response = await GetApiResponseFromContext(context);

            result.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            response!.Message.Should().Be("An internal server error occurred");
        }

        // Tests DbUpdateException returns appropriate 500 error with DB error message.
        [Fact]
        public async Task TryHandleAsync_ShouldReturnExpectedResponse_ForDbUpdateException()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var innerException = new Exception("Unique constraint violation");
            var dbUpdateException = new DbUpdateException("DB error", innerException);

            var result = await _handler.TryHandleAsync(context, dbUpdateException, CancellationToken.None);
            var response = await GetApiResponseFromContext(context);

            result.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            response!.Message.Should().Contain("A database update error occurred: Unique constraint violation");
            response.Result.Should().BeFalse();
            response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            response.Data.Should().BeNull();

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("An error occurred")),
                    dbUpdateException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ), Times.Once);
        }

        // Helper to deserialize ApiResponse from HttpContext response body.
        private static async Task<ApiResponse<object>?> GetApiResponseFromContext(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            return JsonSerializer.Deserialize<ApiResponse<object>>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}
