namespace Common.DTOs
{
    public class AnalysisResultDto
    {
        public int Grade { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
    }

}
