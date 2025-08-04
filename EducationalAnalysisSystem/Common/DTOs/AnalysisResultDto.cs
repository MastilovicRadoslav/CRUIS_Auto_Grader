namespace Common.DTOs
{
    public class AnalysisResultDto
    {
        public int Grade { get; set; }
        public List<string> IdentifiedErrors { get; set; } = new();
        public List<string> ImprovementSuggestions { get; set; } = new();
        public List<string> FurtherRecommendations { get; set; } = new();
    }

}
