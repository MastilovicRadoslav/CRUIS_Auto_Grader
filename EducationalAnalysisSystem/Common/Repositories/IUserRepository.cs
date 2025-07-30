using Common.Models;

namespace Common.Repositories
{
    public interface IUserRepository
    {
        Task InsertUserAsync(User user);
        Task<User?> GetByUsernameAsync(string username);
        Task<List<User>> GetAllUsersAsync();
        Task DeleteByIdAsync(Guid id);
        Task UpdateAsync(User user);
        Task<List<User>> GetAllAsync();
    }
}
