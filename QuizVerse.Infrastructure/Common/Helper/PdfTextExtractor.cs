using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using Docnet.Core;
using Docnet.Core.Models;
using QuizVerse.Infrastructure.Common.Exceptions;
using Tesseract;
using UglyToad.PdfPig;
using SystemImageFormat = System.Drawing.Imaging.ImageFormat;

namespace QuizVerse.Infrastructure.Common.Helper
{
    public static class PdfTextExtractor
    {
        private static string _cachedTessdataPath = string.Empty;

        private static string GetTesseractDataPath()
        {
            if (!string.IsNullOrEmpty(_cachedTessdataPath))
                return _cachedTessdataPath;

            var baseDir = AppContext.BaseDirectory;
            var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            var webRootPath = Path.Combine(projectRoot, "wwwroot", "tessdata");

            if (!Directory.Exists(webRootPath))
                throw new DirectoryNotFoundException(Constants.TESSERACT_DIR_NOT_FOUND);

            var runtimesPath = Path.Combine(AppContext.BaseDirectory, "x64");
            var tesseractDll = Path.Combine(runtimesPath, "tesseract50.dll");
            var leptonicaDll = Path.Combine(runtimesPath, "leptonica-1.82.0.dll");

            if (File.Exists(tesseractDll) && File.Exists(leptonicaDll))
            {
                var path = Environment.GetEnvironmentVariable("PATH");
                if (!path.Contains(runtimesPath))
                {
                    Environment.SetEnvironmentVariable("PATH", path + ";" + runtimesPath);
                }
            }

            _cachedTessdataPath = webRootPath;
            return _cachedTessdataPath;
        }

        public static string ExtractText(string pdfPath, int maxChars = 0)
        {
            var extractedText = new StringBuilder();

            try
            {
                using (var document = PdfDocument.Open(pdfPath))
                {
                    foreach (var page in document.GetPages())
                    {
                        var words = page.GetWords();
                        var pageText = string.Join(" ", words.Select(w => w.Text));

                        if (!string.IsNullOrWhiteSpace(pageText))
                            extractedText.AppendLine(pageText);
                        else
                            extractedText.AppendLine(PerformOcrOnPage(pdfPath, page.Number - 1));

                        // Stop extract if char limit exceed
                        if (maxChars > 0 && extractedText.Length >= maxChars)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new AppException(Constants.ERROR_PROCESSING_PDF);
            }

            var finalText = extractedText.ToString().Trim();
            if (maxChars > 0 && finalText.Length > maxChars)
                finalText = finalText.Substring(0, maxChars);

            return finalText;
        }

        private static string PerformOcrOnPage(string pdfPath, int pageIndex)
        {
            try
            {
                using (var library = DocLib.Instance)
                {
                    using (var docReader = library.GetDocReader(pdfPath, new PageDimensions(1.5)))
                    {
                        using (var pageReader = docReader.GetPageReader(pageIndex))
                        {
                            var rawBytes = pageReader.GetImage();
                            var width = pageReader.GetPageWidth();
                            var height = pageReader.GetPageHeight();

                            using (var bitmap = ConvertRgbaBytesToBitmap(rawBytes, width, height))
                            using (var engine = new TesseractEngine(GetTesseractDataPath(), "eng", EngineMode.Default))
                            {
                                using (var ms = new MemoryStream())
                                {
                                    bitmap.Save(ms, SystemImageFormat.Png);
                                    ms.Position = 0;

                                    using (var pix = Pix.LoadFromMemory(ms.ToArray()))
                                    using (var ocrPage = engine.Process(pix))
                                    {
                                        return ocrPage.GetText();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(Constants.OCR_FAILED_FOR_PAGE, pageIndex + 1));
            }
        }

        private static Bitmap ConvertRgbaBytesToBitmap(byte[] rawBytes, int width, int height)
        {
            try
            {
                var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                var bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    bitmap.PixelFormat);

                System.Runtime.InteropServices.Marshal.Copy(rawBytes, 0, bitmapData.Scan0, rawBytes.Length);
                bitmap.UnlockBits(bitmapData);

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new Exception(Constants.FAILED_TO_CONVERT_IMAGE);
            }
        }
    }
}
