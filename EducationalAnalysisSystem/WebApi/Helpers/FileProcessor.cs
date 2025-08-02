using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.IO.Compression;


namespace WebApi.Helpers
{
    public static class FileProcessor
    {
        public static async Task<string> ExtractTextAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();
            var tempPath = Path.GetTempFileName();

            using (var stream = new FileStream(tempPath, FileMode.Create))
                await file.CopyToAsync(stream);

            return extension switch
            {
                ".txt" => await File.ReadAllTextAsync(tempPath),
                ".pdf" => ExtractTextFromPdf(tempPath),
                ".docx" => ExtractTextFromDocx(tempPath),
                ".zip" => ExtractFromZip(tempPath),
                _ => throw new NotSupportedException("Unsupported file type.")
            };
        }

        private static string ExtractTextFromPdf(string filePath)
        {
            using var pdfReader = new PdfReader(filePath);
            using var pdfDoc = new PdfDocument(pdfReader);
            var strategy = new SimpleTextExtractionStrategy();
            var sb = new System.Text.StringBuilder();

            for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
            {
                var page = pdfDoc.GetPage(i);
                var text = PdfTextExtractor.GetTextFromPage(page, strategy);
                sb.AppendLine(text);
            }

            return sb.ToString();
        }

        private static string ExtractTextFromDocx(string filePath)
        {
            using var doc = WordprocessingDocument.Open(filePath, false);
            return doc.MainDocumentPart?.Document?.InnerText ?? string.Empty;
        }

        private static string ExtractFromZip(string filePath)
        {
            var tempExtractDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            ZipFile.ExtractToDirectory(filePath, tempExtractDir);

            var extractedText = new System.Text.StringBuilder();

            foreach (var file in Directory.GetFiles(tempExtractDir, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(file).ToLower();

                try
                {
                    extractedText.AppendLine(ext switch
                    {
                        ".txt" => File.ReadAllText(file),
                        ".pdf" => ExtractTextFromPdf(file),
                        ".docx" => ExtractTextFromDocx(file),
                        _ => string.Empty
                    });
                }
                catch
                {
                    // ignorisati fajlove koji ne mogu da se parsiraju
                }
            }

            Directory.Delete(tempExtractDir, true);
            return extractedText.ToString();
        }
    }
}
