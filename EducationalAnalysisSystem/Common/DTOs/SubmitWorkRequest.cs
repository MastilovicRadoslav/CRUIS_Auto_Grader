namespace Common.DTOs
{
    public class SubmitWorkRequest
    {
        public Guid StudentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}
