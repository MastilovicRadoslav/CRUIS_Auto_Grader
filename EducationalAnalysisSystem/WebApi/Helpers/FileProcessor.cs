using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace WebApi.Helpers
{
    public static class FileProcessor
    {
        // Safety limiti
        private const long MaxZipTotalBytes = 50L * 1024 * 1024; // 50 MB
        private const int MaxOutputChars = 5_000_000;         // ~5M char cap

        public static async Task<string> ExtractTextAsync(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var tempPath = Path.GetTempFileName();

            try
            {
                using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    await file.CopyToAsync(fs);

                switch (ext)
                {
                    case ".txt":
                    case ".cs":
                    case ".cpp":
                    case ".py":
                    case ".java":
                    case ".js":
                    case ".ts":
                    case ".html":
                    case ".css":
                    case ".json":
                    case ".md":
                        return await ReadAllTextDetectAsync(tempPath);

                    case ".pdf":
                        return ExtractTextFromPdf(tempPath);

                    case ".docx":
                    case ".doc":
                        {
                            // 1) DOC/DOCX → PDF (LibreOffice headless)
                            var pdfPath = await ConvertOfficeToPdfAsync(tempPath);
                            if (pdfPath == null)
                            {
                                // Fallback: .docx može proći preko OpenXML, .doc ne
                                if (ext == ".docx") return ExtractTextFromDocx(tempPath);
                                throw new NotSupportedException(".doc konverzija nije uspjela; instaliraj LibreOffice ili konvertuj fajl u .pdf/.docx.");
                            }
                            try
                            {
                                return ExtractTextFromPdf(pdfPath);
                            }
                            finally
                            {
                                TryDeleteFile(pdfPath);
                            }
                        }

                    case ".zip":
                        return ExtractFromZipSafe(tempPath);

                    default:
                        throw new NotSupportedException($"Unsupported file type: {ext}");
                }
            }
            finally
            {
                TryDeleteFile(tempPath);
            }
        }

        // ---------- PDF ----------
        private static string ExtractTextFromPdf(string filePath)
        {
            var sb = new StringBuilder(capacity: 64 * 1024);
            using var pdfReader = new PdfReader(filePath);
            using var pdfDoc = new PdfDocument(pdfReader);

            int pages = pdfDoc.GetNumberOfPages();
            for (int i = 1; i <= pages; i++)
            {
                // nova strategija po stranici
                ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                var page = pdfDoc.GetPage(i);
                var text = PdfTextExtractor.GetTextFromPage(page, strategy);

                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                    if (sb.Length > MaxOutputChars) break; // safety cap
                }
            }
            return sb.ToString();
        }

        // ---------- DOCX (fallback kad konverzija ne uspije) ----------
        private static string ExtractTextFromDocx(string filePath)
        {
            var sb = new StringBuilder(capacity: 64 * 1024);
            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return string.Empty;

            foreach (var para in body.Elements<Paragraph>())
            {
                foreach (var run in para.Elements<Run>())
                {
                    foreach (var child in run.ChildElements)
                    {
                        switch (child)
                        {
                            case Text t: sb.Append(t.Text); break;
                            case TabChar: sb.Append('\t'); break;
                            case Break: sb.AppendLine(); break;
                        }
                    }
                }
                sb.AppendLine(); // kraj paragrafa
                if (sb.Length > MaxOutputChars) break;
            }
            return sb.ToString();
        }

        // ---------- ZIP (sigurno, sa limitima) ----------
        private static string ExtractFromZipSafe(string zipPath)
        {
            var root = Path.Combine(Path.GetTempPath(), "zip_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            long totalBytes = 0;
            var sb = new StringBuilder(capacity: 64 * 1024);

            try
            {
                using var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var zip = new ZipArchive(fs, ZipArchiveMode.Read);

                foreach (var entry in zip.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue; // folder

                    // ZIP-SLIP zaštita
                    var safeName = entry.FullName.Replace('\\', '/');
                    if (safeName.Contains("..")) continue;

                    using var es = entry.Open();
                    using var ms = new MemoryStream();
                    es.CopyTo(ms);

                    totalBytes += ms.Length;
                    if (totalBytes > MaxZipTotalBytes)
                        throw new InvalidDataException("Zip too large (safety limit).");

                    string? content = null;
                    var ext = Path.GetExtension(entry.Name).ToLowerInvariant();

                    if (ext is ".txt" or ".cs" or ".cpp" or ".py" or ".java" or ".js" or ".ts" or ".html" or ".css" or ".json" or ".md")
                    {
                        content = BytesToStringDetect(ms.ToArray());
                    }
                    else if (ext == ".pdf")
                    {
                        var tmp = WriteTemp(ms.ToArray(), ".pdf");
                        try { content = ExtractTextFromPdf(tmp); }
                        finally { TryDeleteFile(tmp); }
                    }
                    else if (ext is ".docx" or ".doc")
                    {
                        // konverzija u PDF pa parsing
                        var tmp = WriteTemp(ms.ToArray(), ext);
                        try
                        {
                            var pdf = ConvertOfficeToPdfAsync(tmp).GetAwaiter().GetResult();
                            if (pdf != null)
                            {
                                try { content = ExtractTextFromPdf(pdf); }
                                finally { TryDeleteFile(pdf); }
                            }
                            else if (ext == ".docx")
                            {
                                content = ExtractTextFromDocx(tmp); // fallback samo za docx
                            }
                        }
                        finally { TryDeleteFile(tmp); }
                    }

                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        sb.AppendLine($"--- {entry.Name} ---");
                        sb.AppendLine(content);
                        sb.AppendLine();
                        if (sb.Length > MaxOutputChars) break;
                    }
                }

                return sb.ToString();
            }
            finally
            {
                TryDeleteDirectory(root);
            }
        }

        // ---------- LibreOffice konverzija DOC/DOCX → PDF ----------
        /// <summary>
        /// Vraća putanju do generisanog PDF-a ili null ako konverzija ne uspije.
        /// </summary>
        private static async Task<string?> ConvertOfficeToPdfAsync(string inputPath)
        {
            var outDir = Path.Combine(Path.GetTempPath(), "pdf_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outDir);

            var soffice = ResolveSofficePath();
            if (soffice == null) return null;

            var psi = new ProcessStartInfo
            {
                FileName = soffice,
                Arguments = $"--headless --nologo --convert-to pdf --outdir \"{outDir}\" \"{inputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi)!;
            await p.WaitForExitAsync();

            var outPdf = Directory.GetFiles(outDir, "*.pdf").FirstOrDefault();
            if (string.IsNullOrEmpty(outPdf))
            {
                TryDeleteDirectory(outDir);
                return null;
            }

            var finalPdf = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
            File.Copy(outPdf, finalPdf, overwrite: true);
            TryDeleteDirectory(outDir);
            return finalPdf;
        }

        private static string? ResolveSofficePath()
        {
            // 1) env var
            var env = Environment.GetEnvironmentVariable("SOFFICE_PATH");
            if (!string.IsNullOrWhiteSpace(env) && File.Exists(env)) return env;

            // 2) PATH
            var name = OperatingSystem.IsWindows() ? "soffice.exe" : "soffice";
            var paths = (Environment.GetEnvironmentVariable("PATH") ?? "")
                        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in paths)
            {
                var candidate = Path.Combine(p, name);
                if (File.Exists(candidate)) return candidate;
            }

            // 3) tipične lokacije (Windows)
            if (OperatingSystem.IsWindows())
            {
                var guess = new[]
                {
                    @"C:\Program Files\LibreOffice\program\soffice.exe",
                    @"C:\Program Files (x86)\LibreOffice\program\soffice.exe"
                };
                foreach (var g in guess) if (File.Exists(g)) return g;
            }

            // 4) tipične lokacije (Linux)
            if (File.Exists("/usr/bin/soffice")) return "/usr/bin/soffice";
            if (File.Exists("/snap/bin/libreoffice")) return "/snap/bin/libreoffice";

            return null; // nije pronađen
        }

        // ---------- Utilities ----------
        private static async Task<string> ReadAllTextDetectAsync(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(fs, encoding: new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
            var text = await reader.ReadToEndAsync();
            return text.Length > MaxOutputChars ? text[..MaxOutputChars] : text;
        }

        private static string BytesToStringDetect(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            using var reader = new StreamReader(ms, encoding: new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
            var text = reader.ReadToEnd();
            return text.Length > MaxOutputChars ? text[..MaxOutputChars] : text;
        }

        private static string WriteTemp(byte[] bytes, string? ext = null)
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + (ext ?? ""));
            File.WriteAllBytes(path, bytes);
            return path;
        }

        private static void TryDeleteFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
        }

        private static void TryDeleteDirectory(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); } catch { /* ignore */ }
        }
    }
}
