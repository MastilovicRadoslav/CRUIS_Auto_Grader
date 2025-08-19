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

            // 1) Povuci admin settings
            AdminAnalysisSettings settings;
            try
            {
                var userSvc = ServiceProxy.Create<IUserService>(
                    new Uri("fabric:/EducationalAnalysisSystem/UserService"),
                    new ServicePartitionKey(0)
                );
                settings = await userSvc.GetAdminAnalysisSettingsAsync();
            }
            catch
            {
                settings = new AdminAnalysisSettings(); // default 1–10, bez metoda
            }

            try
            {
                analysis = await LlmClient.AnalyzeAsync(work.Content, "", settings);
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
                // 1️⃣ Prvo obriši sve postojeće zapise iz ReliableDictionary
                var enumerator = (await feedbackDict.CreateEnumerableAsync(tx)).GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    await feedbackDict.TryRemoveAsync(tx, enumerator.Current.Key);
                }

                // 2️⃣ Zatim učitaj sve iz MongoDB
                var allFeedbacks = await _feedbackRepo.GetAllAsync();

                // 3️⃣ Ubaci nove zapise u ReliableDictionary
                foreach (var feedback in allFeedbacks)
                {
                    await feedbackDict.AddOrUpdateAsync(tx, feedback.WorkId, feedback, (key, oldValue) => feedback);
                }

                // 4️⃣ Commit
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

        public async Task<EvaluationStatisticsDto> GetStatisticsAsync()
        {
            // 1) Dovuci SVE feedbackove iz MongoDB
            var allFeedbacks = await _feedbackRepo.GetAllAsync();

            // 2) Delegiraj obračun na ProgressService
            var progressService = ServiceProxy.Create<IProgressService>(
                new Uri("fabric:/EducationalAnalysisSystem/ProgressService")
            );

            var stats = await progressService.AnalyzeProgressAsync(allFeedbacks);
            return stats ?? new EvaluationStatisticsDto();
        }


        public async Task<EvaluationStatisticsDto> GetStatisticsByStudentIdAsync(Guid studentId)
        {
            // 1) Dovuci feedback-ove za konkretnog studenta iz MongoDB
            var studentFeedbacks = await _feedbackRepo.GetByStudentIdAsync(studentId);

            // 2) Delegiraj obračun na ProgressService
            var progressService = ServiceProxy.Create<IProgressService>(
                new Uri("fabric:/EducationalAnalysisSystem/ProgressService")
            );

            var stats = await progressService.AnalyzeProgressAsync(studentFeedbacks);
            return stats ?? new EvaluationStatisticsDto();
        }


        public async Task<FeedbackDto?> ReAnalyzeWithInstructionsAsync(ReAnalyzeRequest request) //Testirano - reanaliza feedback
        {
            var submissionService = ServiceProxy.Create<ISubmissionService>(
                new Uri("fabric:/EducationalAnalysisSystem/SubmissionService"),
                new ServicePartitionKey(0)
            );

            var submittedWork = await submissionService.GetWorkByIdAsync(request.WorkId);
            if (submittedWork == null)
                return null;

            AnalysisResultDto? analysis = null;

            // 1) Admin settings
            AdminAnalysisSettings settings;
            try
            {
                var userSvc = ServiceProxy.Create<IUserService>(
                    new Uri("fabric:/EducationalAnalysisSystem/UserService"),
                    new ServicePartitionKey(0)
                );
                settings = await userSvc.GetAdminAnalysisSettingsAsync();
            }
            catch
            {
                settings = new AdminAnalysisSettings();
            }

            try
            {
                analysis = await LlmClient.AnalyzeAsync(submittedWork.Content, request.Instructions, settings);
            }
            catch
            {
                analysis = new AnalysisResultDto
                {
                    Grade = 0,
                    IdentifiedErrors = new List<string> { "Analysis failed." },
                    ImprovementSuggestions = new List<string> { "Try again later." },
                    FurtherRecommendations = new List<string> { "Please consult a professor or teaching assistant for manual review." }
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

        public async Task<int> DeleteFeedbacksByStudentIdAsync(Guid studentId)
        {
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, FeedbackDto>>("feedbacks");
            var toDelete = new List<Guid>();

            // 1) Skupi ključeve
            using (var tx = StateManager.CreateTransaction())
            {
                var en = await dict.CreateEnumerableAsync(tx, EnumerationMode.Unordered);
                var it = en.GetAsyncEnumerator();

                while (await it.MoveNextAsync(CancellationToken.None))
                {
                    var fb = it.Current.Value;
                    if (fb.StudentId == studentId)
                        toDelete.Add(fb.WorkId);
                }
            }

            // 2) Obriši iz Mongo (bulk ako imaš metodu, inače fallback pojedinačno)
            try
            {
                await _feedbackRepo.DeleteManyByStudentIdAsync(studentId);
            }
            catch
            {
                foreach (var wid in toDelete)
                {
                    try { await _feedbackRepo.DeleteAsync(wid); } catch { /* ignore */ }
                }
            }

            // 3) Obriši iz ReliableDictionary
            int dictDeleted = 0;
            using (var tx = StateManager.CreateTransaction())
            {
                foreach (var wid in toDelete)
                {
                    var removed = await dict.TryRemoveAsync(tx, wid);
                    if (removed.HasValue)           // ✅ dovoljno, Value je FeedbackDto
                        dictDeleted++;
                }
                await tx.CommitAsync();
            }

            // (opciono) 4) SignalR notifikacija “progress changed” za tog studenta

            return dictDeleted;
        }

    }
}