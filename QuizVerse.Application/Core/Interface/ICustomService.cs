namespace QuizVerse.Application.Core.Interface;

public interface ICustomService
{
    Task<bool> SendEmail(string userEmail, string subject, string body);
    string Hash(string password);
}

