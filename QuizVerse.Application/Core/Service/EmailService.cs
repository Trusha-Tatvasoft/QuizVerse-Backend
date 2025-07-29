using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;

namespace QuizVerse.Application.Core.Service;

public class EmailService(IConfiguration config) : IEmailService
{
    public virtual async Task<bool> SendEmailAsync(EmailRequestDto emailRequestDto)
    {
        try
        {
            var from = config["EmailSettings:FromAddress"];
            var host = config["EmailSettings:SmtpHost"];
            var port = int.Parse(config["EmailSettings:SmtpPort"] ?? "587");
            var username = config["EmailSettings:SmtpUsername"];
            var password = config["EmailSettings:SmtpPassword"];
            var enableSsl = bool.Parse(config["EmailSettings:EnableSsl"] ?? "true");

            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                throw new AppException(Constants.SMTP_CONFIG_MISSING);

            using var smtpClient = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            using var message = new MailMessage
            {
                From = new MailAddress(from!),
                Subject = emailRequestDto.Subject,
                Body = emailRequestDto.Body,
                IsBodyHtml = true
            };

            message.To.Add(emailRequestDto.To);

            // multiple CCs
            if (emailRequestDto.Cc != null)
            {
                foreach (var cc in emailRequestDto.Cc)
                    message.CC.Add(cc);
            }
            // mutliple Bccs
            if (emailRequestDto.Bcc != null)
            {
                foreach (var bcc in emailRequestDto.Bcc)
                    message.Bcc.Add(bcc);
            }

            await SendAsync(smtpClient, message);
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected virtual Task SendAsync(SmtpClient client, MailMessage message)
    {
        return client.SendMailAsync(message);
    }
}
