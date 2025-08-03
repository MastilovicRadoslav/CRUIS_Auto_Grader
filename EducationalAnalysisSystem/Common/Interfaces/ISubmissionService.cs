using Common.DTOs;
using Common.Enums;
using Common.Models;
using Microsoft.ServiceFabric.Services.Remoting;
using System.ServiceModel;

namespace Common.Interfaces
{
    [ServiceContract]
    public interface ISubmissionService : IService
    {
        [OperationContract]
        Task<OperationResult<Guid>> SubmitWorkAsync(SubmitWorkData request); // Postavljanje svih radova

        [OperationContract]
        Task<List<SubmittedWork>> GetWorksByStudentIdAsync(Guid studentId); // Dobavljanje svih radova za nekog studenta što je postavio

        [OperationContract]
        Task<List<SubmittedWork>> GetAllSubmissionsAsync(); // Pregled svih radova koje su studenti postavili - profesor

        [OperationContract]
        Task<List<SubmittedWork>> GetSubmissionsByStatusAsync(WorkStatus status); // Da vidi sve radove sa Statusom
    }

}
