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
            var password = "TestPassword123!";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var result = _service.VerifyPassword(password, hashedPassword);

            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_ReturnsFalse_WhenPasswordDoesNotMatchHash()
        {
            var password = "TestPassword123!";
            var wrongPassword = "WrongPassword";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var result = _service.VerifyPassword(wrongPassword, hashedPassword);

            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_ReturnsFalse_WhenHashedPasswordIsInvalid()
        {
            var password = "TestPassword123!";
            var invalidHash = "$2a$10$7HPBHeXOqGMn.fDW9mvpaeolxiGERa5xcV1y1Nd66wubh3ee86coW";

            var result = _service.VerifyPassword(password, invalidHash);

            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_ReturnsFalse_WhenPasswordIsEmpty()
        {
            var password = "";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("NonEmpty");

            var result = _service.VerifyPassword(password, hashedPassword);

            Assert.False(result);
        }
    }
}