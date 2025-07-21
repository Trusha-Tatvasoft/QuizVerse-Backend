using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using QuizVerse.Infrastructure.ApiResponse;
using QuizVerse.Infrastructure.Common.Exceptions;

namespace QuizVerse.WebAPI.Middlewares;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var queryString = httpContext.Request.QueryString.HasValue ? httpContext.Request.QueryString.Value : string.Empty;

        _logger.LogError(exception,
            "An error occurred while processing request {Method} {Path} on {MachineName} with trace ID : {TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path + queryString,
            Environment.MachineName,
            traceId);

        var (statusCode, message) = MapException(exception);

        var response = new ApiResponse<object>
        {
            Result = false,
            Message = message,
            StatusCode = statusCode,
            Data = null
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }

    private static (int statusCode, string message) MapException(Exception exception)
    {
        return exception switch
        {
            AppException ex => (ex.StatusCode, ex.Message),

            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized access"),

            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),

            NotImplementedException => (StatusCodes.Status501NotImplemented, "This feature is not implemented yet"),

            ArgumentNullException ex => (StatusCodes.Status400BadRequest, $"Missing required argument: {ex.ParamName}"),

            ArgumentException ex => (StatusCodes.Status400BadRequest, $"Invalid argument: {ex.Message}"),

            InvalidOperationException ex => (StatusCodes.Status409Conflict, $"Invalid operation: {ex.Message}"),

            TimeoutException => (StatusCodes.Status408RequestTimeout, "Request timed out"),

            BadHttpRequestException ex => (StatusCodes.Status400BadRequest, $"Bad request: {ex.Message}"),

            FileNotFoundException ex => (StatusCodes.Status404NotFound, $"File not found: {ex.FileName ?? ex.Message}"),

            IOException ex => (StatusCodes.Status500InternalServerError, $"I/O error: {ex.Message}"),

            DbUpdateException dbEx => (
                StatusCodes.Status500InternalServerError,
                $"A database update error occurred: {dbEx.InnerException?.Message ?? dbEx.Message}"
            ),

            _ => (StatusCodes.Status500InternalServerError, "An internal server error occurred")
        };
    }
}
