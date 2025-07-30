using Common.DTOs;
using Common.Models;
using Microsoft.ServiceFabric.Services.Remoting;
using System.ServiceModel;

namespace Common.Interfaces
{
    [ServiceContract]
    public interface IUserService : IService
    {
        [OperationContract]
        Task<OperationResult<Guid>> RegisterUserAsync(RegisterRequest request); // Metoda za registraciju

        [OperationContract]
        Task<User?> LoginAsync(LoginRequest request); // Metoda za logovanje
    }
}
