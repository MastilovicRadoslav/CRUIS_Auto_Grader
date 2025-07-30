using Common.Configurations;
using Common.DTOs;
using Common.Enums;
using Common.Interfaces;
using Common.Models;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Fabric;

namespace SubmissionService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class SubmissionService : StatefulService, ISubmissionService
    {
        private readonly SubmissionMongoRepository _mongoRepo;

        public SubmissionService(StatefulServiceContext context) : base(context)
        {
            var submissionSettings = new UserDbSettings
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "EducationalSystemDb",
                CollectionName = "Submissions"
            };

            _mongoRepo = new SubmissionMongoRepository(submissionSettings);
        }

        public async Task<OperationResult<Guid>> SubmitWorkAsync(SubmitWorkRequest request)
        {
            var submissions = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, SubmittedWork>>("submissions");

            var newSubmission = new SubmittedWork
            {
                Id = Guid.NewGuid(),
                StudentId = request.StudentId,
                Title = request.Title,
                Content = request.Content,
                SubmittedAt = DateTime.UtcNow,
                Status = WorkStatus.Pending
            };

            try
            {
                // 1. Upis rada u Mongo
                await _mongoRepo.InsertAsync(newSubmission);

                // 2. Upis rada u ReliableDictionary
                using (var tx = StateManager.CreateTransaction())
                {
                    await submissions.AddAsync(tx, newSubmission.Id, newSubmission);
                    await tx.CommitAsync();
                }

                // 3. Evaluacija
                var evaluationService = ServiceProxy.Create<IEvaluationService>(
                    new Uri("fabric:/EducationalAnalysisSystem/EvaluationService"),
                    new ServicePartitionKey(0)
                );

                var feedback = await evaluationService.EvaluateAsync(newSubmission);

                // 4. Promjena statusa i ažuriranje u Mongo + ReliableDictionary
                newSubmission.Status = WorkStatus.Completed;

                // Update u Mongo (moraš imati metodu UpdateStatusByIdAsync)
                await _mongoRepo.UpdateStatusByIdAsync(newSubmission.Id, WorkStatus.Completed);

                // Update u ReliableDictionary
                using (var tx = StateManager.CreateTransaction())
                {
                    await submissions.SetAsync(tx, newSubmission.Id, newSubmission);
                    await tx.CommitAsync();
                }

                return OperationResult<Guid>.Ok(newSubmission.Id);
            }
            catch (Exception ex)
            {
                await _mongoRepo.DeleteByIdAsync(newSubmission.Id); // ako postoji ta metoda
                return OperationResult<Guid>.Fail("Error saving submission: " + ex.Message);
            }
        }


        public async Task<List<SubmittedWork>> GetAllSubmissionsAsync()
        {
            var submissions = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, SubmittedWork>>("submissions");
            var result = new List<SubmittedWork>();

            using (var tx = StateManager.CreateTransaction())
            {
                var enumerable = await submissions.CreateEnumerableAsync(tx);
                var enumerator = enumerable.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    result.Add(enumerator.Current.Value);
                }
            }

            return result;
        }


        public async Task<List<SubmittedWork>> GetWorksByStudentIdAsync(Guid studentId)
        {
            var submissions = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, SubmittedWork>>("submissions");
            var result = new List<SubmittedWork>();

            using (var tx = StateManager.CreateTransaction())
            {
                var enumerable = await submissions.CreateEnumerableAsync(tx);
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

        private async Task LoadSubmissionsAsync()
        {
            var submissionsDict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, SubmittedWork>>("submissions");

            using (var tx = StateManager.CreateTransaction())
            {
                var allFromMongo = await _mongoRepo.GetAllAsync();

                foreach (var submission in allFromMongo)
                {
                    await submissionsDict.AddOrUpdateAsync(tx, submission.Id, submission, (key, oldValue) => submission);
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

            await LoadSubmissionsAsync();

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
    }
}
