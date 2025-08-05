using ClosedXML.Excel;

namespace QuizVerse.Application.Core.Interface
{
    public interface ICommonService
    {
        string Hash(string password);
        bool VerifyPassword(string password, string hashedPassword);
        DateTime ToDate(string dateString);
        MemoryStream ExportToExcel<T>(List<T> data, string sheetName, XLTableTheme? tableTheme, int startRow = 10, int startCol = 1, Action<IXLWorksheet>? setup = null);
    }
}
