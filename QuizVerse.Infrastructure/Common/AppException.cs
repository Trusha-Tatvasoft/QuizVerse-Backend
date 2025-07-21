namespace QuizVerse.Infrastructure.Common.Exceptions;
public class AppException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
