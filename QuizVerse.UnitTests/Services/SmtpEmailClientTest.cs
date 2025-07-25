using System.Net.Mail;
using System.Threading.Tasks;
using Xunit;
using QuizVerse.Application.Core.Service;

namespace QuizVerse.UnitTests.Services;
public class SmtpEmailClientTest
{
    [Fact]
    public async Task SendAsync_WithoutConfigure_ThrowsInvalidOperationException()
    {
        var emailClient = new SmtpEmailClient();

        var message = new MailMessage("test@example.com", "to@example.com", "Subject", "Body");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => emailClient.SendAsync(message));
        Assert.Equal("Email client not configured.", ex.Message);
    }

    [Fact]
    public void Configure_SetsSmtpClient_WithoutThrowing()
    {
        var emailClient = new SmtpEmailClient();

        emailClient.Configure("smtp.example.com", 587, "user", "pass", true);
    }
}
