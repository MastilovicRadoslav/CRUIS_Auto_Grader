namespace Common.DTOs
{
    public class EvaluationStatisticsDto
    {
        public int TotalWorks { get; set; }                        // Ukupan broj radova
        public double AverageGrade { get; set; }                   // Prosečna ocjena
        public Dictionary<int, int> GradeDistribution { get; set; } = new(); // Raspodjela ocjena (npr. {6: 2, 7: 4, ...})
        public List<Tuple<DateTime, int>> GradeTimeline { get; set; } = new(); // Evolucija ocjena kroz vrijeme
        public Dictionary<string, int> MostCommonIssues { get; set; } = new(); // Najčešće greške

        public int Above9 { get; set; }       // Broj radova sa ocjenom ≥ 90
        public int Between7And8 { get; set; } // Broj radova sa ocjenom 70–89
        public int Below7 { get; set; }        // Broj radova sa ocjenom < 70
    }
}
