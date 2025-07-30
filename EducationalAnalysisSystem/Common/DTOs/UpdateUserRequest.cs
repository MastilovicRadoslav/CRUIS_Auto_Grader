using Common.Enums;

namespace Common.DTOs
{
    public class UpdateUserRequest
    {
        public string? NewPassword { get; set; }  // može biti null ako se ne menja
        public Role? NewRole { get; set; }        // može biti null ako se ne menja
    }

}
