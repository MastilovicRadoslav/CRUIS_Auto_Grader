using Common.DTOs;
using Common.Models;
using Microsoft.ServiceFabric.Services.Remoting;
using System.ServiceModel;

namespace Common.Interfaces
{
    [ServiceContract]
    public interface IEvaluationService : IService
    {
        [OperationContract]
        Task<FeedbackDto> EvaluateAsync(SubmittedWork work); // Čuva radove studenta, obradjuje ih u EvaluationService...

        [OperationContract]
        Task<List<FeedbackDto>> GetFeedbacksByStudentIdAsync(Guid studentId); // Vraća sve feedback-ove koji pripadaju datom studentu. 

        [OperationContract]
        Task<bool> AddProfessorCommentAsync(AddProfessorCommentRequest request); // Metoda za dodavanje komentara na rad od strane profesora

        [OperationContract]
        Task<FeedbackDto?> GetFeedbackByWorkIdAsync(Guid workId); // Pregled feedback-a sa komentarima za konkretan rad

        [OperationContract]
        Task<List<FeedbackDto>> GetAllFeedbacksAsync(); // Profesor vidi sve feedback-ove koje je sistem generisao, Kasnije ćemo dodati: po studentu, po datumu, po oceni...

        [OperationContract]
        Task<EvaluationStatisticsDto> GetStatisticsAsync(); // Statistički izveštaji za profesora

        [OperationContract]
        Task<EvaluationStatisticsDto> GetStatisticsByStudentIdAsync(Guid studentId); // Filtriranje statistike po studentu

        [OperationContract]
        Task<EvaluationStatisticsDto> GetStatisticsByDateRangeAsync(DateRangeRequest request); //Statistika evaluacija u zadatom vremenskom opsegu


    }
}
