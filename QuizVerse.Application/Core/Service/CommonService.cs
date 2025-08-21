using System.Globalization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.Common;

namespace QuizVerse.Application.Core.Service
{
    public class CommonService : ICommonService
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
    }
}