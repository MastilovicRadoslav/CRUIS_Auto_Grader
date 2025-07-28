using Common.DTOs;
using Common.Models;
using Microsoft.ServiceFabric.Services.Remoting;
using System.ServiceModel;

namespace Common.Interfaces
{
    [ServiceContract]
    public interface IUserService : IService
    {
        Task<Guid> RegisterUserAsync(RegisterRequest request);

        Task<User?> LoginAsync(LoginRequest request);
    }
}
