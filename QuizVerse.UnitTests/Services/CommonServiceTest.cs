using System.Linq.Expressions;
using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Application.Core.Service;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;
using Xunit;

namespace QuizVerse.UnitTests.Services
{
    public class CommonServiceTests
    {
        private readonly Mock<IGenericRepository<User>> _userRepositoryMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IGenericRepository<EmailTemplete>> _emailTemplateRepositoryMock = new();
        private readonly CommonService _service;

        public CommonServiceTests()
        {
            _service = new CommonService(
                _userRepositoryMock.Object,
                _emailServiceMock.Object,
                _emailTemplateRepositoryMock.Object
            );
        }

        #region PasswordHash
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
        #endregion

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

        #region ExportToExcel

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

        #region FileUpload
        [Fact]
        public async Task SaveFile_ShouldReturnNull_WhenFileIsNull()
        {
            var result = await _service.SaveFile(null!, "test");
            result.Should().BeNull();
        }

        [Fact]
        public async Task SaveFile_ShouldReturnNull_WhenFileIsEmpty()
        {
            var fileMock = new FormFile(Stream.Null, 0, 0, "Data", "empty.txt");
            var result = await _service.SaveFile(fileMock, "test");

            result.Should().BeNull();
        }

        [Fact]
        public async Task SaveFile_ShouldSaveFileAndReturnRelativePath()
        {
            string folderName = "unittestfiles";
            string fileContent = "Hello Test";
            byte[] fileBytes = Encoding.UTF8.GetBytes(fileContent);
            using var stream = new MemoryStream(fileBytes);

            var formFile = new FormFile(stream, 0, fileBytes.Length, "Data", "testfile.txt")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };

            var result = await _service.SaveFile(formFile, folderName);

            result.Should().NotBeNull();
            result.Should().Contain(folderName);
            result.Should().EndWith(".txt");

            var savedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", result.Replace("/", Path.DirectorySeparatorChar.ToString()));
            File.Exists(savedFilePath).Should().BeTrue();

            if (File.Exists(savedFilePath))
                File.Delete(savedFilePath);
        }
        #endregion

        #region DeleteFile
        [Fact]
        public void DeleteFile_ShouldReturnFalse_WhenPathIsNull()
        {
            var result = _service.DeleteFile(null!);
            result.Should().BeFalse();
        }

        [Fact]
        public void DeleteFile_ShouldReturnFalse_WhenPathIsEmpty()
        {
            var result = _service.DeleteFile(string.Empty);
            result.Should().BeFalse();
        }

        [Fact]
        public void DeleteFile_ShouldReturnFalse_WhenFileDoesNotExist()
        {
            string fakePath = "unittestfiles/nonexistent.txt";
            var result = _service.DeleteFile(fakePath);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteFile_ShouldReturnTrue_WhenFileExistsAndDeletedSuccessfully()
        {
            // Arrange: create a file using SaveFile to ensure structure is consistent
            string folderName = "unittestfiles";
            string fileContent = "Temporary File";
            byte[] fileBytes = Encoding.UTF8.GetBytes(fileContent);
            using var stream = new MemoryStream(fileBytes);

            var formFile = new FormFile(stream, 0, fileBytes.Length, "Data", "tempfile.txt")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };

            var relativePath = await _service.SaveFile(formFile, folderName);
            relativePath.Should().NotBeNull();

            // Act
            var result = _service.DeleteFile(relativePath!);

            // Assert
            result.Should().BeTrue();

            string fullPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                relativePath!.Replace("/", Path.DirectorySeparatorChar.ToString())
            );
            File.Exists(fullPath).Should().BeFalse();
        }

        [Fact]
        public void DeleteFile_ShouldReturnFalse_WhenExceptionThrown()
        {
            // Arrange
            // Passing invalid path with illegal characters to trigger exception
            string invalidPath = "invalid<>path/test.txt";

            // Act
            var result = _service.DeleteFile(invalidPath);

            // Assert
            result.Should().BeFalse();
        }
        #endregion


        #region Create CSV Helper
        [Fact]
        public void EscapeCsv_NullInput_ReturnsEmptyString()
        {
            // Arrange
            string? input = null;

            // Act
            var result = _service.EscapeCsv(input!);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void EscapeCsv_EmptyInput_ReturnsEmptyString()
        {
            // Arrange
            string input = "";

            // Act
            var result = _service.EscapeCsv(input);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void EscapeCsv_NoSpecialCharacters_ReturnsInputUnchanged()
        {
            // Arrange
            string input = "Hello World";

            // Act
            var result = _service.EscapeCsv(input);

            // Assert
            result.Should().Be("Hello World");
        }

        [Fact]
        public void EscapeCsv_WithComma_WrapsInQuotes()
        {
            // Arrange
            string input = "Hello,World";

            // Act
            var result = _service.EscapeCsv(input);

            // Assert
            result.Should().Be("\"Hello,World\"");
        }

        [Fact]
        public void EscapeCsv_WithQuote_EscapesQuoteAndWrapsInQuotes()
        {
            // Arrange
            string input = "He said \"Hello\"";

            // Act
            var result = _service.EscapeCsv(input);

            // Assert
            result.Should().Be("\"He said \"\"Hello\"\"\"");
        }

        [Fact]
        public void EscapeCsv_WithNewline_WrapsInQuotes()
        {
            // Arrange
            string input = "Hello\nWorld";

            // Act
            var result = _service.EscapeCsv(input);

            // Assert
            result.Should().Be("\"Hello\nWorld\"");
        }

        [Fact]
        public void EscapeCsv_WithMultipleQuotes_EscapesAllQuotesAndWrapsInQuotes()
        {
            // Arrange
            string input = "Quote\"Test\"Quote";

            // Act
            var result = _service.EscapeCsv(input);

            // Assert
            result.Should().Be("\"Quote\"\"Test\"\"Quote\"");
        }

        [Fact]
        public void EscapeCsv_WithCommaAndQuote_EscapesQuoteAndWrapsInQuotes()
        {
            // Arrange
            string input = "Hello,\"World\"";

            // Act
            var result = _service.EscapeCsv(input);

            // Assert
            result.Should().Be("\"Hello,\"\"World\"\"\"");
        }

        [Fact]
        public void EscapeCsv_WithAllSpecialCharacters_EscapesQuoteAndWrapsInQuotes()
        {
            // Arrange
            string input = "Hello,\n\"World\"";

            // Act
            var result = _service.EscapeCsv(input);

            // Assert
            result.Should().Be("\"Hello,\n\"\"World\"\"\"");
        }
        #endregion

        #region GenerateOtp
        [Fact]
        public async Task GenerateOtp_ShouldCreateOtp_AndUpdateUser_WhenUserExists()
        {
            var user = new User { Email = "test@test.com" };
            _userRepositoryMock
                .Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), null))
                .ReturnsAsync(user);

            _userRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            var otp = await _service.GenerateOtp("test@test.com");

            otp.Should().NotBeNullOrEmpty();
            otp.Length.Should().Be(6);
            user.Otp.Should().Be(otp);
            user.OtpSentDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            _userRepositoryMock.Verify(r => r.UpdateAsync(It.Is<User>(u => u.Otp == otp)), Times.Once);
        }

        [Fact]
        public async Task GenerateOtp_ShouldReturnOtp_WhenUserDoesNotExist()
        {
            _userRepositoryMock
                .Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), null))
                .ReturnsAsync((User?)null);

            var otp = await _service.GenerateOtp("notfound@test.com");

            otp.Should().NotBeNullOrEmpty();
            otp.Length.Should().Be(6);

            _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }
        #endregion

        #region SendEmailFromTemplate

        [Fact]
        public async Task SendEmailFromTemplate_TemplateNotFound_ThrowsAppException()
        {
            var dto = new TemplatedEmailRequestDto
            {
                TemplateType = EmailTemplateType.NewUser,
                ToEmail = "test@example.com",
                Placeholders = new Dictionary<string, string>()
            };

            _emailTemplateRepositoryMock.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<EmailTemplete, bool>>>(),
                It.IsAny<Func<IQueryable<EmailTemplete>, IQueryable<EmailTemplete>>?>()));

            var act = () => _service.SendEmailFromTemplate(dto);

            await act.Should().ThrowAsync<AppException>()
                .WithMessage(Constants.EMAIL_TEMPLATE_NOT_FOUND);
        }

        [Fact]
        public async Task SendEmailFromTemplate_EmptyTemplateBody_ReturnsBodyEmptyConstant()
        {
            var template = new EmailTemplete
            {
                TemplateType = (int)EmailTemplateType.NewUser,
                Body = "",
                Status = true,
                IsDeleted = false
            };

            var dto = new TemplatedEmailRequestDto
            {
                TemplateType = EmailTemplateType.NewUser,
                ToEmail = "test@example.com",
                Placeholders = new Dictionary<string, string>()
            };

            _emailTemplateRepositoryMock
      .Setup(r => r.GetAsync(
          It.IsAny<Expression<Func<EmailTemplete, bool>>>(),
          It.IsAny<Func<IQueryable<EmailTemplete>, IQueryable<EmailTemplete>>?>()))
      .ReturnsAsync(template);


            var result = await _service.SendEmailFromTemplate(dto);

            result.Should().Be(Constants.EMAIL_BODY_EMPTY);
        }

        public static IEnumerable<object[]> MissingPlaceholderCases =>
            [
        [
            new TemplatedEmailRequestDto
            {
                TemplateType = EmailTemplateType.NewUser,
                ToEmail = "test@example.com",
                Placeholders = new Dictionary<string, string> { { "{{user}}", "john@example.com" } }
            },
            "{{password}}"
        ],
        [
            new TemplatedEmailRequestDto
            {
                TemplateType = EmailTemplateType.NewUser,
                ToEmail = "test@example.com",
                Placeholders = new Dictionary<string, string> { { "{{password}}", "1234" } }
            },
            "{{user}}"
        ]
            ];

        [Theory]
        [MemberData(nameof(MissingPlaceholderCases))]
        public async Task SendEmailFromTemplate_MissingRequiredPlaceholders_ThrowsAppException(
            TemplatedEmailRequestDto dto, string missingPlaceholder)
        {
            var template = new EmailTemplete
            {
                TemplateType = (int)EmailTemplateType.NewUser,
                Body = "Hello {{user}}, your password is {{password}}.",
                Subject = "Welcome",
                Status = true,
                IsDeleted = false
            };

            Constants.EmailTemplatePlaceholdersRequired[EmailTemplateType.NewUser] =
                ["{{user}}", "{{password}}"];

            _emailTemplateRepositoryMock
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<EmailTemplete, bool>>>(),
                    It.IsAny<Func<IQueryable<EmailTemplete>, IQueryable<EmailTemplete>>?>()))
                .ReturnsAsync(template);


            var act = () => _service.SendEmailFromTemplate(dto);

            await act.Should().ThrowAsync<AppException>()
                .WithMessage(string.Format(Constants.EMAIL_PLACEHOLDER_MISSING, missingPlaceholder));
        }

        [Fact]
        public async Task SendEmailFromTemplate_EmailSendFails_ReturnsFailureMessage()
        {
            var dto = new TemplatedEmailRequestDto
            {
                TemplateType = EmailTemplateType.NewUser,
                ToEmail = "test@example.com",
                Placeholders = new Dictionary<string, string>
        {
            { "{{user}}", "john@example.com" },
            { "{{password}}", "1234" }
        }
            };

            var template = new EmailTemplete
            {
                TemplateType = (int)EmailTemplateType.NewUser,
                Body = "Hello {{user}}, your password is {{password}}.",
                Subject = "Welcome",
                Status = true,
                IsDeleted = false
            };

            Constants.EmailTemplatePlaceholdersRequired[EmailTemplateType.NewUser] =
                ["{{user}}", "{{password}}"];

            _emailTemplateRepositoryMock
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<EmailTemplete, bool>>>(),
                    It.IsAny<Func<IQueryable<EmailTemplete>, IQueryable<EmailTemplete>>?>()))
                .ReturnsAsync(template);

            _emailServiceMock
                .Setup(e => e.SendEmailAsync(It.IsAny<EmailRequestDto>()))
                .ReturnsAsync(false);

            var result = await _service.SendEmailFromTemplate(dto);

            result.Should().Be(Constants.EMAIL_NOT_SENT);
        }

        [Fact]
        public async Task SendEmailFromTemplate_EmailSendSuccess_ReturnsSuccessMessage()
        {
            var dto = new TemplatedEmailRequestDto
            {
                TemplateType = EmailTemplateType.NewUser,
                ToEmail = "test@example.com",
                Placeholders = new Dictionary<string, string>
                {
                    { "{{user}}", "john@example.com" },
                    { "{{password}}", "1234" }
                }
            };

            var template = new EmailTemplete
            {
                TemplateType = (int)EmailTemplateType.NewUser,
                Body = "Hello {{user}}, your password is {{password}}.",
                Subject = "Welcome",
                Status = true,
                IsDeleted = false
            };

            Constants.EmailTemplatePlaceholdersRequired[EmailTemplateType.NewUser] =
                ["{{user}}", "{{password}}"];

            _emailTemplateRepositoryMock
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<EmailTemplete, bool>>>(),
                    It.IsAny<Func<IQueryable<EmailTemplete>, IQueryable<EmailTemplete>>?>()))
                .ReturnsAsync(template);


            _emailServiceMock
                .Setup(e => e.SendEmailAsync(It.IsAny<EmailRequestDto>()))
                .ReturnsAsync(true);

            var result = await _service.SendEmailFromTemplate(dto);

            result.Should().Be(string.Format(Constants.EMAIL_SENT_SUCCESS, dto.ToEmail));
        }

        #endregion
    }
}