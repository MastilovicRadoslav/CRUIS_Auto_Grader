using Common.DTOs;
using System.ServiceModel;

namespace Common.Interfaces
{
    [ServiceContract]
    public interface IEvaluationEngineService
    {
        [OperationContract]
        Task<FeedbackDto> AnalyzeAsync(Guid workId, Guid studentId, string fileName, byte[] contentBytes);
    }

}
