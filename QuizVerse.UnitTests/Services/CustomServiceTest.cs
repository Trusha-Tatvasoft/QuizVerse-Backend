using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Common.Exceptions;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class CustomServiceTest
    {
        [Fact]
        public async Task SendEmail_ReturnsTrue_WhenEmailClientSucceeds()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["EmailSettings:FromAddress"]).Returns("sender@example.com");
            mockConfig.Setup(c => c["EmailSettings:SmtpHost"]).Returns("smtp.example.com");
            mockConfig.Setup(c => c["EmailSettings:SmtpPort"]).Returns("587");
            mockConfig.Setup(c => c["EmailSettings:SmtpUsername"]).Returns("user");
            mockConfig.Setup(c => c["EmailSettings:SmtpPassword"]).Returns("pass");
            mockConfig.Setup(c => c["EmailSettings:EnableSsl"]).Returns("true");

            var mockEmailClient = new Mock<IEmailClient>();
            mockEmailClient.Setup(e => e.Configure(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()));
            mockEmailClient.Setup(e => e.SendAsync(It.IsAny<MailMessage>())).Returns(Task.CompletedTask);

            var service = new CustomService(mockConfig.Object, mockEmailClient.Object);

            // Act
            var result = await service.SendEmail("receiver@example.com", "Test Subject", "<p>Body</p>");

            // Assert
            Assert.True(result);

            mockEmailClient.Verify(e => e.Configure("smtp.example.com", 587, "user", "pass", true), Times.Once);
            mockEmailClient.Verify(e => e.SendAsync(It.IsAny<MailMessage>()), Times.Once);
        }

        [Fact]
        public async Task SendEmail_ReturnsFalse_WhenExceptionOccurs()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["EmailSettings:FromAddress"]).Returns("sender@example.com");
            mockConfig.Setup(c => c["EmailSettings:SmtpHost"]).Returns("smtp.example.com");
            mockConfig.Setup(c => c["EmailSettings:SmtpPort"]).Returns("587");
            mockConfig.Setup(c => c["EmailSettings:SmtpUsername"]).Returns("user");
            mockConfig.Setup(c => c["EmailSettings:SmtpPassword"]).Returns("pass");
            mockConfig.Setup(c => c["EmailSettings:EnableSsl"]).Returns("true");

            var mockEmailClient = new Mock<IEmailClient>();
            mockEmailClient.Setup(e => e.Configure(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()));
            mockEmailClient.Setup(e => e.SendAsync(It.IsAny<MailMessage>())).ThrowsAsync(new Exception("SMTP failed"));

            var service = new CustomService(mockConfig.Object, mockEmailClient.Object);

            // Act
            var result = await service.SendEmail("receiver@example.com", "Test Subject", "<p>Body</p>");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SendEmail_ReturnsFalse_WhenConfigIsMissing()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["EmailSettings:FromAddress"]).Returns((string?)null); // missing
            mockConfig.Setup(c => c["EmailSettings:SmtpHost"]).Returns("smtp.example.com");
            mockConfig.Setup(c => c["EmailSettings:SmtpPort"]).Returns("587");
            mockConfig.Setup(c => c["EmailSettings:SmtpUsername"]).Returns("user");
            mockConfig.Setup(c => c["EmailSettings:SmtpPassword"]).Returns("pass");
            mockConfig.Setup(c => c["EmailSettings:EnableSsl"]).Returns("true");

            var mockEmailClient = new Mock<IEmailClient>();
            var service = new CustomService(mockConfig.Object, mockEmailClient.Object);

            // Act
            var result = await service.SendEmail("receiver@example.com", "Subject", "<p>Body</p>");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Hash_ReturnsHashedPassword_And_VerifiesSuccessfully()
        {
            var password = "Test@123";
            var service = new CustomService(null!, null!);
            var hashedPassword = service.Hash(password);
            Assert.False(string.IsNullOrWhiteSpace(hashedPassword));
            Assert.True(BCrypt.Net.BCrypt.Verify(password, hashedPassword));
        }

    }
}
