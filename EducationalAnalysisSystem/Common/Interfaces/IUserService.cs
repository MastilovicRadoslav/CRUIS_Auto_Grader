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

        [OperationContract]
        Task<List<User>> GetAllUsersAsync(); // Dobavljanje svih korisnika

        [OperationContract]
        Task<OperationResult<Guid>> CreateUserAsync(CreateUserRequest request); //Kreisanje User-a

        [OperationContract]
        Task<OperationResult<bool>> DeleteUserAsync(Guid userId); // Brisanje User-a

        [OperationContract]
        Task<OperationResult<bool>> UpdateUserAsync(Guid userId, UpdateUserRequest request); //Azuriranje studenta

        [OperationContract]
        Task<OperationResult<bool>> SetMaxSubmissionsAsync(int max);

        [OperationContract]
        Task<int?> GetMaxSubmissionsAsync();

        [OperationContract]
        Task<string?> GetStudentNameByIdAsync(Guid studentId);

    }
}
