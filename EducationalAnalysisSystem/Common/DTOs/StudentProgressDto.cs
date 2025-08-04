namespace Common.DTOs
{
    public class StudentProgressDto
    {
        public int TotalWorks { get; set; }
        public double AverageGrade { get; set; }
        public Dictionary<int, int> GradeDistribution { get; set; } = new();
        public List<Tuple<DateTime, int>> GradeTimeline { get; set; } = new();
    }

}
