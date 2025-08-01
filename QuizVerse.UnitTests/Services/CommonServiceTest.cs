using ClosedXML.Excel;
using FluentAssertions;
using QuizVerse.Application.Core.Service;
using QuizVerse.Infrastructure.Common;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class CommonServiceTests
    {
        private readonly CommonService _service = new();


        [Fact]
        public void Hash_ReturnsHashedPassword_WhenValidPasswordProvided()
        {
            var password = "MySecret123!";
            var hashed = _service.Hash(password);
            Assert.False(string.IsNullOrWhiteSpace(hashed));
            Assert.StartsWith("$2", hashed);
        }

        [Fact]
        public void Hash_ReturnsDifferentHashes_ForSamePassword()
        {
            var password = "RepeatablePassword";
            var hash1 = _service.Hash(password);
            var hash2 = _service.Hash(password);
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void Hash_CanBeVerifiedWithVerifyPassword()
        {
            var password = "VerifyMe!";
            var hashed = _service.Hash(password);
            var result = _service.VerifyPassword(password, hashed);
            Assert.True(result);
        }

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

        [Fact]
        public void ToDate_ParsesValidDateString_Correctly()
        {
            string input = "2025-07-30";

            DateTime result = _service.ToDate(input);

            Assert.Equal(new DateTime(2025, 7, 30), result);
        }

        [Fact]
        public void ToDate_NullInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.ToDate(null!));
        }

        [Theory]
        [InlineData("")]
        [InlineData("07-30-2025")]
        [InlineData("2025/07/30")]
        [InlineData("July 30, 2025")]
        public void ToDate_InvalidDateFormat_ThrowsFormatException(string? invalidInput)
        {
            Assert.Throws<FormatException>(() => _service.ToDate(invalidInput!));
        }
        #region ExportToExcel Tests


        [Fact]
        public void ExportToExcel_WithAnonymousType_ShouldReturnNonEmptyStream()
        {
            // Arrange
            var data = new List<object>
        {
            new { Id = 1, Name = "Alice" },
            new { Id = 2, Name = "Bob" }
        };

            // Act
            var stream = _service.ExportToExcel(data, "TestSheet", XLTableTheme.TableStyleLight9);

            // Assert
            stream.Should().NotBeNull();
            stream.Length.Should().BeGreaterThan(0);

            using var workbook = new XLWorkbook(stream);
            workbook.Worksheets.Contains("TestSheet").Should().BeTrue();
        }

        [Fact]
        public void ExportToExcel_ShouldApplySetupActionCorrectly()
        {
            // Arrange
            var data = new List<object> { new { Id = 1, Name = "Test" } };
            bool wasCalled = false;

            // Act
            var stream = _service.ExportToExcel(data, "CustomSheet", XLTableTheme.TableStyleLight8, 10, 1, ws =>
            {
                ws.Cell("C3").Value = "Custom Value";
                wasCalled = true;
            });

            // Assert
            wasCalled.Should().BeTrue();
            using var workbook = new XLWorkbook(stream);
            var value = workbook.Worksheet("CustomSheet").Cell("C3").Value.ToString();
            value.Should().Be("Custom Value");
        }

        [Fact]
        public void ExportToExcel_WithEmptyData_ShouldStillReturnValidExcel()
        {
            // Arrange
            var emptyData = new List<object>();

            // Act
            var stream = _service.ExportToExcel(emptyData, "EmptySheet", XLTableTheme.TableStyleMedium6);

            // Assert
            stream.Should().NotBeNull();
            stream.Length.Should().BeGreaterThan(0);

            using var workbook = new XLWorkbook(stream);
            workbook.Worksheet("EmptySheet").Should().NotBeNull();
        }

        [Fact]
        public void ExportToExcel_WithTheme_ShouldApplyCorrectStyle()
        {
            // Arrange
            var data = new List<object>
        {
            new { Id = 1, Name = "Styled" }
        };

            var theme = XLTableTheme.TableStyleMedium2;

            // Act
            var stream = _service.ExportToExcel(data, "StyledSheet", theme);

            // Assert
            stream.Should().NotBeNull();

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet("StyledSheet");
            var table = worksheet.Tables.Table("StyledSheetTable");
            table.Theme.Should().Be(theme);
        }

        [Fact]
        public void ExportToExcel_WhenLogoExists_ShouldInsertLogoImage()
        {
            // Arrange
            var data = new List<object> { new { Id = 1, Name = "TestUser" } };
            var expectedPath = "wwwroot/images/logo.png";

            Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);

            // Write a tiny transparent PNG
            byte[] transparentPng = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGNgYAAAAAMAAWgmWQ0AAAAASUVORK5CYII="
            );
            File.WriteAllBytes(expectedPath, transparentPng);

            // Act
            var stream = _service.ExportToExcel(data, "LogoSheet", XLTableTheme.TableStyleDark1);

            // Assert
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet("LogoSheet");
            worksheet.Pictures.Count.Should().BeGreaterThan(0);

            File.Delete(expectedPath);
        }
        #endregion
    }
}