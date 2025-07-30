namespace Common.DTOs
{
    public class EvaluationStatisticsDto
    {
        public int TotalWorks { get; set; }
        public double AverageGrade { get; set; }
        public int Above90 { get; set; }
        public int Between70And89 { get; set; }
        public int Below70 { get; set; }
        public Dictionary<string, int> MostCommonIssues { get; set; } = new();
    }

}
