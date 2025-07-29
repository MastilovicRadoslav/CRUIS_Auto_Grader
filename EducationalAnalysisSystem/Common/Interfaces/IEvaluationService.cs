using Common.DTOs;
using Common.Models;
using Microsoft.ServiceFabric.Services.Remoting;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Common.Interfaces
{
    [ServiceContract]
    public interface IEvaluationService : IService
    {
        [OperationContract]
        Task<FeedbackDto> EvaluateAsync(SubmittedWork work); // Čuva radove studenta, obradjuje ih u EvaluationService...

        [OperationContract]
        Task<List<FeedbackDto>> GetFeedbacksByStudentIdAsync(Guid studentId); // Vraća sve feedback-ove koji pripadaju datom studentu. 

    }
}
