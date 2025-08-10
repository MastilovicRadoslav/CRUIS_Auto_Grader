// Common/DTOs/AdminAnalysisSettings.cs
namespace Common.DTOs
{
    public class AdminAnalysisSettings
    {
        public int MinGrade { get; set; } = 1;            // npr. 1
        public int MaxGrade { get; set; } = 10;           // npr. 10
        public List<string> Methods { get; set; } = new(); // npr. ["grammar","plagiarism","code-style","complexity"]
    }
}
