using System.Globalization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service
{
    public class CommonService(IGenericRepository<User> userRepository, IEmailService emailService, IGenericRepository<EmailTemplete> emailTemplateRepository) : ICommonService
    {
        #region PasswordHash
        public string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        #endregion PasswordHash

        #region DateParsing
        public DateTime ToDate(string dateString)
        {
            return DateTime.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;
        }
        #endregion DateParsing

        #region Excel Export
        public MemoryStream ExportToExcel<T>(List<T> data, string sheetName, XLTableTheme? tableTheme, int startRow = 10, int startCol = 1, Action<IXLWorksheet>? setup = null)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(sheetName);

            string logoPath = Constants.LOGO_PATH;
            if (File.Exists(logoPath))
            {
                ws.AddPicture(logoPath).MoveTo(ws.Cell("D2")).WithSize(320, 70);
            }

            setup?.Invoke(ws);

            // Insert table
            var table = ws.Cell(startRow, startCol).InsertTable(data, sheetName + "Table", true);
            table.Theme = tableTheme ?? XLTableTheme.TableStyleMedium2;
            table.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            table.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Columns().AdjustToContents();

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }
        #endregion Excel Export

        #region File Upload
        public async Task<string?> SaveFile(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0) return null;

            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folderName);
            Directory.CreateDirectory(wwwrootPath);

            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(wwwrootPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string finalFileName = Path.Combine(folderName, fileName).Replace("\\", "/");
            return finalFileName;
        }

        public bool DeleteFile(string relativeFilePath)
        {
            if (string.IsNullOrWhiteSpace(relativeFilePath))
                return false;

            try
            {
                string fullPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    relativeFilePath
                );
                fullPath = Path.GetFullPath(fullPath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Create CSV Helper
        public string EscapeCsv(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            bool mustQuote = input.Contains(",") || input.Contains("\"") || input.Contains("\n");
            if (mustQuote)
            {
                // Escape quotes by doubling them
                input = input.Replace("\"", "\"\"");
                return $"\"{input}\"";
            }

            return input;
        }
        #endregion

        #region GenerateOtp
        public async Task<string> GenerateOtp(string email)
        {
            var user = await userRepository.GetAsync(u => u.Email.ToLower().Trim() == email.ToLower().Trim());

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();

            if (user != null)
            {
                user.Otp = otp;
                user.OtpSentDate = DateTime.UtcNow;
                await userRepository.UpdateAsync(user);
            }
            return otp;
        }
        #endregion

        #region SendTemplatedEmail
        public async Task<string> SendEmailFromTemplate(TemplatedEmailRequestDto dto)
        {
            var template = await emailTemplateRepository.GetAsync(t => t.TemplateType == (int)dto.TemplateType && t.Status && !t.IsDeleted)
                ?? throw new AppException(Constants.EMAIL_TEMPLATE_NOT_FOUND);

            if (string.IsNullOrWhiteSpace(template.Body))
                return Constants.EMAIL_BODY_EMPTY;

            // Validate required placeholders
            if (Constants.EmailTemplatePlaceholdersRequired.TryGetValue(dto.TemplateType, out var requiredPlaceholders))
            {
                foreach (string placeholder in requiredPlaceholders)
                {
                    if (!dto.Placeholders.ContainsKey(placeholder))
                        throw new AppException(string.Format(Constants.EMAIL_PLACEHOLDER_MISSING, placeholder));
                }
            }

            // Replace placeholders
            string body = template.Body;
            foreach (var kvp in dto.Placeholders)
            {
                body = body.Replace(kvp.Key, kvp.Value);
            }

            var emailSent = await emailService.SendEmailAsync(new EmailRequestDto
            {
                To = dto.ToEmail,
                Subject = template.Subject,
                Body = body
            });

            return emailSent
                ? string.Format(Constants.EMAIL_SENT_SUCCESS, dto.ToEmail)
                : Constants.EMAIL_NOT_SENT;
        }
        #endregion
    }
}