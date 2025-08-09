using Common.Configurations;
using Common.DTOs;
using Common.Helpers;
using Common.Interfaces;
using Common.Models;
using EvaluationService.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Fabric;
using System.Net.Http.Json;


namespace EvaluationService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class EvaluationService : StatefulService, IEvaluationService
    {
        private readonly FeedbackMongoRepository _feedbackRepo;

        public EvaluationService(StatefulServiceContext context) : base(context)
        {
            var settings = new UserDbSettings
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "EducationalSystemDb",
                CollectionName = "Feedbacks"
            };

            _feedbackRepo = new FeedbackMongoRepository(settings);
        }
        public async Task<FeedbackDto> EvaluateAsync(SubmittedWork work)
        {
            AnalysisResultDto? analysis = null;

            try
            {
                analysis = await LlmClient.AnalyzeAsync(work.Content);
            }
            catch (Exception ex)
            {
                // fallback analiza
                analysis = new AnalysisResultDto
                {
                    Grade = 0,
                    IdentifiedErrors = new List<string> { "Analysis failed." },
                    ImprovementSuggestions = new List<string> { "Try again later." },
                    FurtherRecommendations = new List<string> { "Please consult a professor or teaching assistant for manual review." }
                };
            }

            var feedback = new FeedbackDto
            {
                WorkId = work.Id,
                Title = work.Title,
                StudentId = work.StudentId,
                StudentName = work.StudentName,
                Grade = analysis.Grade,
                IdentifiedErrors = analysis.IdentifiedErrors,
                ImprovementSuggestions = analysis.ImprovementSuggestions,
                FurtherRecommendations = analysis.FurtherRecommendations,
                EvaluatedAt = DateTime.UtcNow
            };


            try
            {
                await _feedbackRepo.InsertAsync(feedback);

                var feedbackDict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, FeedbackDto>>("feedbacks");

                using (var tx = StateManager.CreateTransaction())
                {
                    await feedbackDict.AddOrUpdateAsync(tx, feedback.WorkId, feedback, (key, oldValue) => feedback);
                    await tx.CommitAsync();
                }

                try
                {
                    var httpClient = new HttpClient();
                    var progressNotification = new ProgressUpdateDto
                    {
                        StudentId = feedback.StudentId
                    };

                    await httpClient.PostAsJsonAsync("http://localhost:8285/api/evaluation/notify-progress-change", progressNotification);
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.ServiceMessage(this.Context, "Progress SignalR notification failed: " + ex.Message);
                }


                return feedback;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "Error during feedback evaluation: {0}", ex.Message);
                throw;
            }
        }


        public async Task<bool> AddProfessorCommentAsync(AddProfessorCommentRequest request) // Dodavanje komentara od strane profesora na Feedback
        {
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, FeedbackDto>>("feedbacks");

            using (var tx = StateManager.CreateTransaction())
            {
                var result = await dict.TryGetValueAsync(tx, request.WorkId);
                if (!result.HasValue)
                    return false;

                var feedback = result.Value;
                feedback.ProfessorComment = request.Comment;

                await dict.SetAsync(tx, request.WorkId, feedback);
                await tx.CommitAsync();

                await _feedbackRepo.UpdateAsync(request.WorkId, feedback);
                return true;
            }
        }

        private async Task LoadFeedbacksAsync()
        {
            var feedbackDict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, FeedbackDto>>("feedbacks");

            using (var tx = StateManager.CreateTransaction())
            {
                var allFeedbacks = await _feedbackRepo.GetAllAsync();

                foreach (var feedback in allFeedbacks)
                {
                    await feedbackDict.AddOrUpdateAsync(tx, feedback.WorkId, feedback, (key, oldValue) => feedback);
                }

                await tx.CommitAsync();
            }
        }



        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
            => this.CreateServiceRemotingReplicaListeners();

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            await LoadFeedbacksAsync(); // 

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        public async Task<List<FeedbackDto>> GetFeedbacksByStudentIdAsync(Guid studentId)
        {
            var feedbackDict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, FeedbackDto>>("feedbacks");
            var result = new List<FeedbackDto>();

            using (var tx = StateManager.CreateTransaction())
            {
                var enumerable = await feedbackDict.CreateEnumerableAsync(tx);
                var enumerator = enumerable.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    if (enumerator.Current.Value.StudentId == studentId)
                    {
                        result.Add(enumerator.Current.Value);
                    }
                }
            }

            return result;
        }

        public async Task<FeedbackDto?> GetFeedbackByWorkIdAsync(Guid workId) // Testirano
        {
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, FeedbackDto>>("feedbacks");

            using (var tx = StateManager.CreateTransaction())
            {
                var result = await dict.TryGetValueAsync(tx, workId);
                return result.HasValue ? result.Value : null;
            }
        }

        public async Task<List<FeedbackDto>> GetAllFeedbacksAsync()
        {
            var feedbacks = new List<FeedbackDto>();
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, FeedbackDto>>("feedbacks");

            using (var tx = StateManager.CreateTransaction())
            {
                var enumerable = await dict.CreateEnumerableAsync(tx);
                var enumerator = enumerable.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    feedbacks.Add(enumerator.Current.Value);
                }
            }

            return feedbacks;
        }

        public async Task<EvaluationStatisticsDto> GetStatisticsAsync() // Testirano - Dobavljanje statistike za sve studente iz ProgressService
        {
            // 1) Pokupi sve feedback-ove iz ReliableDictionary-a
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, FeedbackDto>>("feedbacks");
            var allFeedbacks = new List<FeedbackDto>();

            using (var tx = StateManager.CreateTransaction())
            {
                var enumerable = await dict.CreateEnumerableAsync(tx);
                var enumerator = enumerable.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    allFeedbacks.Add(enumerator.Current.Value);
                }
            }

            // 2) Delegiraj računanje na ProgressService
            var progressService = ServiceProxy.Create<IProgressService>(
                new Uri("fabric:/EducationalAnalysisSystem/ProgressService")
            );

            var stats = await progressService.AnalyzeProgressAsync(allFeedbacks);

            // 3) Vrati rezultat
            return stats ?? new EvaluationStatisticsDto();
        }


        public async Task<EvaluationStatisticsDto> GetStatisticsByStudentIdAsync(Guid studentId) // Testirano - Dobavljanje statistike za jednog studenta iz ProgressService
        {
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, FeedbackDto>>("feedbacks");
            var result = new EvaluationStatisticsDto();

            var feedbackList = new List<FeedbackDto>();

            using (var tx = StateManager.CreateTransaction())
            {
                var enumerable = await dict.CreateEnumerableAsync(tx);
                var enumerator = enumerable.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    var feedback = enumerator.Current.Value;
                    if (feedback.StudentId == studentId)
                    {
                        feedbackList.Add(feedback);
                    }
                }
            }

            var progressService = ServiceProxy.Create<IProgressService>(
                new Uri("fabric:/EducationalAnalysisSystem/ProgressService")
            );

            result = await progressService.AnalyzeProgressAsync(feedbackList);

            return result;
        }

        public async Task<FeedbackDto?> ReAnalyzeWithInstructionsAsync(ReAnalyzeRequest request) //Testirano - reanaliya feedback
        {
            var submissionService = ServiceProxy.Create<ISubmissionService>(
                new Uri("fabric:/EducationalAnalysisSystem/SubmissionService"),
                new ServicePartitionKey(0)
            );

            var submittedWork = await submissionService.GetWorkByIdAsync(request.WorkId);
            if (submittedWork == null)
                return null;

            AnalysisResultDto? analysis = null;

            try
            {
                analysis = await LlmClient.AnalyzeAsync(submittedWork.Content, request.Instructions);
            }
            catch
            {
                analysis = new AnalysisResultDto
                {
                    Grade = 0,
                    IdentifiedErrors = new List<string> { "Analysis failed." },
                    ImprovementSuggestions = new List<string> { "Try again later." },
                    FurtherRecommendations = new List<string> { "Consult a professor for detailed analysis." }
                };
            }

            // 🔁 Čitanje prethodnog feedbacka iz ReliableDictionary da sačuvaš ProfessorComment
            var feedbackDict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, FeedbackDto>>("feedbacks");
            FeedbackDto? previousFeedback = null;

            using (var tx = StateManager.CreateTransaction())
            {
                var result = await feedbackDict.TryGetValueAsync(tx, submittedWork.Id);
                if (result.HasValue)
                    previousFeedback = result.Value;
            }

            // 🆕 Formiraj novi feedback sa sačuvanim prethodnim komentarom
            var feedback = new FeedbackDto
            {
                WorkId = submittedWork.Id,
                Title = submittedWork.Title,
                StudentId = submittedWork.StudentId,
                StudentName = submittedWork.StudentName,
                Grade = analysis.Grade,
                IdentifiedErrors = analysis.IdentifiedErrors,
                ImprovementSuggestions = analysis.ImprovementSuggestions,
                FurtherRecommendations = analysis.FurtherRecommendations,
                ProfessorComment = previousFeedback?.ProfessorComment, // sačuvan komentar
                EvaluatedAt = DateTime.UtcNow
            };

            // 📝 Upis u ReliableDictionary
            using (var tx = StateManager.CreateTransaction())
            {
                await feedbackDict.SetAsync(tx, feedback.WorkId, feedback);
                await tx.CommitAsync();
            }

            // 📝 Update Mongo
            await _feedbackRepo.UpdateAsync(feedback.WorkId, feedback);

            // 🔔 SignalR notifikacija - obavijesti o napretku
            try
            {
                var httpClient = new HttpClient();
                var progressNotification = new ProgressUpdateDto
                {
                    StudentId = feedback.StudentId
                };

                await httpClient.PostAsJsonAsync("http://localhost:8285/api/evaluation/notify-progress-change", progressNotification);
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "Progress SignalR notification failed: " + ex.Message);
            }

            return feedback;
        }

        public async Task<EvaluationStatisticsDto> GetStatisticsByFiltersAsync(ReportFilterRequest request) //Testirano, funkcija koja na osnovu Data-Range dava statistiku ili za sve studente ili za jednog studenta
        {
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, FeedbackDto>>("feedbacks");
            var all = new List<FeedbackDto>();
            using (var tx = StateManager.CreateTransaction())
            {
                var en = await dict.CreateEnumerableAsync(tx);
                var it = en.GetAsyncEnumerator();
                while (await it.MoveNextAsync(CancellationToken.None))
                    all.Add(it.Current.Value);
            }

            var progressService = ServiceProxy.Create<IProgressService>(
                new Uri("fabric:/EducationalAnalysisSystem/ProgressService")
            );
            return await progressService.AnalyzeByFiltersAsync(all, request);
        }


    }
}