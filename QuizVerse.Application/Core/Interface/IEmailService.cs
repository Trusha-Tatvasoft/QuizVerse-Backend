using QuizVerse.Infrastructure.DTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IEmailService
{
    Task<bool> SendEmailAsync(EmailRequestDto email);
}
