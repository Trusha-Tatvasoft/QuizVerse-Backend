// using System.Net.Mail;
// using System.Threading.Tasks;
// using Microsoft.Extensions.Configuration;
// using Moq;
// using QuizVerse.Application.Core.Service;
// using QuizVerse.Infrastructure.DTOs;
// using Xunit;

// namespace QuizVerse.Tests.Services
// {
//     public class EmailServiceTests
//     {
//         private readonly Mock<IConfiguration> _mockConfig;

//         public EmailServiceTests()
//         {
//             _mockConfig = new Mock<IConfiguration>();
//             _mockConfig.Setup(c => c["EmailSettings:FromAddress"]).Returns("test@domain.com");
//             _mockConfig.Setup(c => c["EmailSettings:SmtpHost"]).Returns("smtp.domain.com");
//             _mockConfig.Setup(c => c["EmailSettings:SmtpPort"]).Returns("587");
//             _mockConfig.Setup(c => c["EmailSettings:SmtpUsername"]).Returns("username");
//             _mockConfig.Setup(c => c["EmailSettings:SmtpPassword"]).Returns("password");
//             _mockConfig.Setup(c => c["EmailSettings:EnableSsl"]).Returns("true");
//         }

//         [Fact]
//         public async Task SendEmailAsync_ValidRequest_ReturnsTrue()
//         {
//             // Arrange
//             var emailRequest = new EmailRequestDto
//             {
//                 To = "recipient@domain.com",
//                 Subject = "Test Subject",
//                 Body = "<h1>Hello</h1>"
//             };

//             var service = new TestableEmailService(_mockConfig.Object);

//             // Act
//             var result = await service.SendEmailAsync(emailRequest);

//             // Assert
//             Assert.True(result);
//             Assert.True(service.SendAsyncCalled); // Custom flag to ensure SendAsync was invoked
//         }

//         [Fact]
//         public async Task SendEmailAsync_Exception_ReturnsFalse()
//         {
//             // Arrange
//             var emailRequest = new EmailRequestDto
//             {
//                 To = "recipient@domain.com",
//                 Subject = "Test Subject",
//                 Body = "<h1>Hello</h1>"
//             };

//             var service = new TestableEmailService(_mockConfig.Object, throwOnSend: true);

//             // Act
//             var result = await service.SendEmailAsync(emailRequest);

//             // Assert
//             Assert.False(result);
//         }

//         // Custom testable subclass
//         private class TestableEmailService : EmailService
//         {
//             private readonly bool _throwOnSend;
//             public bool SendAsyncCalled { get; private set; }

//             public TestableEmailService(IConfiguration config, bool throwOnSend = false)
//                 : base(config)
//             {
//                 _throwOnSend = throwOnSend;
//             }

//             protected override Task SendAsync(SmtpClient client, MailMessage message)
//             {
//                 SendAsyncCalled = true;

//                 if (_throwOnSend)
//                     throw new SmtpException("Simulated failure");

//                 // Do not actually send anything
//                 return Task.CompletedTask;
//             }
//         }
//     }
// }



using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Moq;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs;
using Xunit;

namespace QuizVerse.UnitTests.Services;

public class EmailServiceTest
{
    private readonly IConfiguration _validConfig;
    private readonly EmailRequestDto _validEmailRequest;

    public EmailServiceTest()
    {
        _validConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailSettings:FromAddress"] = "from@example.com",
                ["EmailSettings:SmtpHost"] = "smtp.example.com",
                ["EmailSettings:SmtpPort"] = "587",
                ["EmailSettings:SmtpUsername"] = "username",
                ["EmailSettings:SmtpPassword"] = "password",
                ["EmailSettings:EnableSsl"] = "true"
            })
            .Build();

        _validEmailRequest = new EmailRequestDto
        {
            To = "to@example.com",
            Subject = "Test Subject",
            Body = "<p>Test Body</p>",
            Cc = ["cc1@example.com", "cc2@example.com"],
            Bcc = ["bcc1@example.com"]
        };
    }

    [Fact]
    public async Task SendEmailAsync_ShouldReturnTrue_WhenEmailIsSentSuccessfully()
    {
        var emailService = new TestableEmailService(_validConfig);
        var result = await emailService.SendEmailAsync(_validEmailRequest);

        Assert.True(result);
        Assert.True(emailService.SendCalled); // Ensure SendAsync was invoked
    }

    [Fact]
    public async Task SendEmailAsync_ShouldReturnFalse_WhenSendFails()
    {
        var emailService = new TestableEmailService(_validConfig, throwOnSend: true);

        var result = await emailService.SendEmailAsync(_validEmailRequest);

        Assert.False(result);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrowAppException_WhenConfigMissing()
    {
        var badConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EmailSettings:FromAddress"] = "",
            ["EmailSettings:SmtpHost"] = "",
            ["EmailSettings:SmtpUsername"] = "",
            ["EmailSettings:SmtpPassword"] = ""
        }).Build();

        var emailService = new EmailService(badConfig);

        var result = await emailService.SendEmailAsync(_validEmailRequest);

        Assert.False(result);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldHandleNullCcAndBccGracefully()
    {
        var request = new EmailRequestDto
        {
            To = "to@example.com",
            Subject = "Subject",
            Body = "<p>Body</p>",
            Cc = null,
            Bcc = null
        };

        var emailService = new TestableEmailService(_validConfig);

        var result = await emailService.SendEmailAsync(request);

        Assert.True(result);
    }

    private class TestableEmailService(IConfiguration config, bool throwOnSend = false) : EmailService(config)
    {
        public bool SendCalled { get; private set; }

        protected override Task SendAsync(SmtpClient client, MailMessage message)
        {
            SendCalled = true;

            if (throwOnSend)
                throw new Exception("Simulated send failure");

            return Task.CompletedTask;
        }
    }
}
