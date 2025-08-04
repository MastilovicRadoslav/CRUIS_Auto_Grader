using Common.DTOs;
using Common.Models;
using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interfaces
{
    [ServiceContract]
    public interface IProgressService : IService
    {
        [OperationContract]
        Task<EvaluationStatisticsDto> AnalyzeProgressAsync(List<FeedbackDto> works);
    }

}
