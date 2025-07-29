using Common.DTOs;
using Common.Models;
using Microsoft.ServiceFabric.Services.Remoting;
using System.ServiceModel;

namespace Common.Interfaces
{
    [ServiceContract]
    public interface ISubmissionService : IService
    {
        [OperationContract]
        Task<OperationResult<Guid>> SubmitWorkAsync(SubmitWorkRequest request);

        [OperationContract]
        Task<List<SubmittedWork>> GetWorksByStudentIdAsync(Guid studentId);
    }

}
