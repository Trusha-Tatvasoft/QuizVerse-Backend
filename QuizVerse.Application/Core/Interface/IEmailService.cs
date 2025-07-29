using QuizVerse.Infrastructure.DTOs.RequestDTOs;

namespace QuizVerse.Application.Core.Interface;

public interface IEmailService
{
    Task<bool> SendEmailAsync(EmailRequestDto email);
}
