using QuizVerse.Application.Core.Service;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class CommonServiceTests
    {
        private readonly CommonService _service = new();

        [Fact]
        public void VerifyPassword_ReturnsTrue_WhenPasswordMatchesHash()
        {
            // Arrange
            var password = "TestPassword123!";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            // Act
            var result = _service.VerifyPassword(password, hashedPassword);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_ReturnsFalse_WhenPasswordDoesNotMatchHash()
        {
            // Arrange
            var password = "TestPassword123!";
            var wrongPassword = "WrongPassword";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            // Act
            var result = _service.VerifyPassword(wrongPassword, hashedPassword);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_ReturnsFalse_WhenHashedPasswordIsInvalid()
        {
            // Arrange
            var password = "TestPassword123!";
            var invalidHash = "$2a$10$7HPBHeXOqGMn.fDW9mvpaeolxiGERa5xcV1y1Nd66wubh3ee86coW";

            // Act
            var result = _service.VerifyPassword(password, invalidHash);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_ReturnsFalse_WhenPasswordIsEmpty()
        {
            // Arrange
            var password = "";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("NonEmpty");

            // Act
            var result = _service.VerifyPassword(password, hashedPassword);

            // Assert
            Assert.False(result);
        }
    }
}