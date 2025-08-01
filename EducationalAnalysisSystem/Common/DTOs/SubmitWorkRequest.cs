namespace Common.DTOs
{
    public class SubmitWorkRequest
    {
        public Guid StudentId { get; set; }
        public string Content { get; set; } = string.Empty; // Kasnije ce biti za razlicite fajlove
        public string Title { get; set; } = string.Empty;
    }
}
