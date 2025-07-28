using Common.Enums;

namespace Common.DTOs
{
    public class LoginResponse
    {
        public Guid UserId { get; set; }
        public Role Role { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
