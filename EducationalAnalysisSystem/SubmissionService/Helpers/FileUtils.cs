using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubmissionService.Helpers
{
    internal class FileUtils
    {
        public static string ExtractTextFromFile(string filename, byte[] fileBytes)
        {
            var extension = Path.GetExtension(filename).ToLowerInvariant();

            return extension switch
            {
                //".txt" => Encoding.UTF8.GetString(fileBytes),
                //".pdf" => PdfTextExtractor.Extract(fileBytes),
                //".docx" => WordTextExtractor.Extract(fileBytes),
                //".zip" => "[ZIP file uploaded]", // Obrada ZIP fajlova kasnije
                //_ => throw new InvalidOperationException("Unsupported file type")
            };
        }

    }
}
