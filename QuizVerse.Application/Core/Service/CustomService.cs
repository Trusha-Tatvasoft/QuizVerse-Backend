using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.Common.Exceptions;

namespace QuizVerse.Application.Core.Service;

public class CustomService(IConfiguration config, IEmailClient emailClient) : ICustomService
{
    private readonly IConfiguration _config = config;
    private readonly IEmailClient _emailClient = emailClient;


    #region SendMail
    public async Task<bool> SendEmail(string userEmail, string subject, string body)
    {
        try
        {
            var fromAddress = _config["EmailSettings:FromAddress"];
            var smtpHost = _config["EmailSettings:SmtpHost"];
            var smtpPort = _config["EmailSettings:SmtpPort"];
            var smtpUsername = _config["EmailSettings:SmtpUsername"];
            var smtpPassword = _config["EmailSettings:SmtpPassword"];
            var enableSsl = _config["EmailSettings:EnableSsl"];

            if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(smtpHost) ||
                string.IsNullOrEmpty(smtpPort) || string.IsNullOrEmpty(smtpUsername) ||
                string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(enableSsl))
            {
                throw new AppException("Email settings are missing or incorrect in the configuration.");
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(new MailAddress(userEmail));

            _emailClient.Configure(smtpHost, int.Parse(smtpPort), smtpUsername, smtpPassword, bool.Parse(enableSsl));

            await _emailClient.SendAsync(mailMessage);
            return true;
        }
        catch
        {
            return false;
        }
    }
    #endregion SendMail

    #region PasswordHash
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
    #endregion PasswordHash
}
