namespace Common.DTOs
{
    public class ReAnalyzeRequest
    {
        public Guid WorkId { get; set; }
        public string Instructions { get; set; } = string.Empty;
    }

}
