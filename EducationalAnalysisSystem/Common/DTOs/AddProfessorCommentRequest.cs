namespace Common.DTOs
{
    public class AddProfessorCommentRequest
    {
        public Guid WorkId { get; set; }
        public string Comment { get; set; } = string.Empty;
    }

}
