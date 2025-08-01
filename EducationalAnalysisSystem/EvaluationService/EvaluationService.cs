using Common.Configurations;
using Common.DTOs;
using Common.Interfaces;
using Common.Models;
using EvaluationService.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Fabric;


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
            var feedback = new FeedbackDto
            {
                WorkId = work.Id,
                Title = work.Title,
                StudentId = work.StudentId,
                StudentName = work.StudentName,
                Grade = 85,
                Issues = new List<string> { "No major issues detected." },
                Suggestions = new List<string> { "Consider expanding the conclusion." },
                Summary = "Basic evaluation completed successfully.",
                EvaluatedAt = DateTime.UtcNow
            };


            try
            {
                // 1. Upis u Mongo
                await _feedbackRepo.InsertAsync(feedback);

                // 2. Upis u ReliableDictionary
                var feedbackDict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, FeedbackDto>>("feedbacks");

                using (var tx = StateManager.CreateTransaction())
                {
                    await feedbackDict.AddOrUpdateAsync(tx, feedback.WorkId, feedback, (key, oldValue) => feedback);
                    await tx.CommitAsync();
                }

                return feedback;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "Error during feedback evaluation: {0}", ex.Message);
                throw;
            }
        }

        public async Task<bool> AddProfessorCommentAsync(AddProfessorCommentRequest request)
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

        public async Task<FeedbackDto?> GetFeedbackByWorkIdAsync(Guid workId)
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
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, FeedbackDto>>("feedbacks");
            var result = new EvaluationStatisticsDto();

            var allGrades = new List<int>();
            var issueCounter = new Dictionary<string, int>();

            using (var tx = StateManager.CreateTransaction())
            {
                var enumerable = await dict.CreateEnumerableAsync(tx);
                var enumerator = enumerable.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    var feedback = enumerator.Current.Value;
                    var grade = feedback.Grade;
                    allGrades.Add(grade);

                    if (grade >= 90) result.Above90++;
                    else if (grade >= 70) result.Between70And89++;
                    else result.Below70++;

                    foreach (var issue in feedback.Issues ?? new List<string>())
                    {
                        if (issueCounter.ContainsKey(issue))
                            issueCounter[issue]++;
                        else
                            issueCounter[issue] = 1;
                    }
                }
            }

            result.TotalWorks = allGrades.Count;
            result.AverageGrade = allGrades.Any() ? allGrades.Average() : 0;
            result.MostCommonIssues = issueCounter;

            return result;
        }

        public async Task<EvaluationStatisticsDto> GetStatisticsByStudentIdAsync(Guid studentId)
        {
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, FeedbackDto>>("feedbacks");
            var result = new EvaluationStatisticsDto();

            var allGrades = new List<int>();
            var issueCounter = new Dictionary<string, int>();

            using (var tx = StateManager.CreateTransaction())
            {
                var enumerable = await dict.CreateEnumerableAsync(tx);
                var enumerator = enumerable.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    var feedback = enumerator.Current.Value;

                    if (feedback.StudentId != studentId)
                        continue;

                    var grade = feedback.Grade;
                    allGrades.Add(grade);

                    if (grade >= 90) result.Above90++;
                    else if (grade >= 70) result.Between70And89++;
                    else result.Below70++;

                    foreach (var issue in feedback.Issues ?? new List<string>())
                    {
                        if (issueCounter.ContainsKey(issue))
                            issueCounter[issue]++;
                        else
                            issueCounter[issue] = 1;
                    }
                }
            }

            result.TotalWorks = allGrades.Count;
            result.AverageGrade = allGrades.Any() ? allGrades.Average() : 0;
            result.MostCommonIssues = issueCounter;

            return result;
        }

        public async Task<EvaluationStatisticsDto> GetStatisticsByDateRangeAsync(DateRangeRequest request)
        {
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, FeedbackDto>>("feedbacks");
            var result = new EvaluationStatisticsDto();
            var grades = new List<int>();
            var issues = new Dictionary<string, int>();

            using (var tx = StateManager.CreateTransaction())
            {
                var enumerable = await dict.CreateEnumerableAsync(tx);
                var enumerator = enumerable.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    var feedback = enumerator.Current.Value;

                    // Ako si već dodao EvaluatedAt u FeedbackDto, onda:
                    if (feedback.EvaluatedAt >= request.From && feedback.EvaluatedAt <= request.To)
                    {
                        var grade = feedback.Grade;
                        grades.Add(grade);

                        if (grade >= 90) result.Above90++;
                        else if (grade >= 70) result.Between70And89++;
                        else result.Below70++;

                        foreach (var issue in feedback.Issues ?? new List<string>())
                        {
                            if (issues.ContainsKey(issue)) issues[issue]++;
                            else issues[issue] = 1;
                        }
                    }
                }
            }

            result.TotalWorks = grades.Count;
            result.AverageGrade = grades.Any() ? grades.Average() : 0;
            result.MostCommonIssues = issues;

            return result;
        }

    }
}
