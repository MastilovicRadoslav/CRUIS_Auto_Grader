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
                StudentId = work.StudentId,
                Grade = 85,
                Issues = new List<string> { "No major issues detected." },
                Suggestions = new List<string> { "Consider expanding the conclusion." },
                Summary = "Basic evaluation completed successfully."
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

    }
}
