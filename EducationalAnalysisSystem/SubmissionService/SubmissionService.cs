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
using System.Net.Http.Json;

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

        public async Task<OperationResult<Guid>> SubmitWorkAsync(SubmitWorkData request) // Radi
        {
            var submissions = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, SubmittedWork>>("submissions");

            //Dobavljanje imena studenta na osnovu Id
            var userService = ServiceProxy.Create<IUserService>(
                new Uri("fabric:/EducationalAnalysisSystem/UserService"),
                new ServicePartitionKey(0)
            );

            var studentName = await userService.GetStudentNameByIdAsync(request.StudentId);

            var estimatedTime = EstimateAnalysisTime(request.Content);

            var newSubmission = new SubmittedWork
            {
                Id = Guid.NewGuid(),
                StudentId = request.StudentId,
                Title = request.Title,
                Content = request.Content, // 
                EstimatedAnalysisTime = estimatedTime,
                StudentName = studentName ?? "Unknown",
                SubmittedAt = DateTime.UtcNow,
                Status = WorkStatus.InProgress
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

                // ✅ Prva SignalR notifikacija odmah nakon upisa
                try
                {
                    var httpClient = new HttpClient();
                    var notification = new StatusChangeNotificationDto
                    {
                        WorkId = newSubmission.Id,
                        NewStatus = newSubmission.Status,
                        Title = newSubmission.Title,
                        EstimatedAnalysisTime = newSubmission.EstimatedAnalysisTime,
                        SubmittedAt = newSubmission.SubmittedAt,
                        StudentName = newSubmission.StudentName
                    };

                    await httpClient.PostAsJsonAsync("http://localhost:8285/api/submission/notify-status-change", notification);
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.ServiceMessage(this.Context, "Initial SignalR notification failed: " + ex.Message);
                }


                // 3. Evaluacija
                var evaluationService = ServiceProxy.Create<IEvaluationService>(
                    new Uri("fabric:/EducationalAnalysisSystem/EvaluationService"),
                    new ServicePartitionKey(0)
                );

                var feedback = await evaluationService.EvaluateAsync(newSubmission);

                //// 4. Promjena statusa i ažuriranje u Mongo + ReliableDictionary
                if (feedback.Grade == 0)
                {
                    newSubmission.Status = WorkStatus.Rejected;
                }
                else
                {
                    newSubmission.Status = WorkStatus.Completed;
                }

                //newSubmission.EstimatedAnalysisTime = TimeSpan.Zero; // Ako Sladjana kaze da treba da se resetuje na 0 nakon analize ili da se uzivo prati oduzimanje onda cemo nesto jos dodati



                //// Update u Mongo (moraš imati metodu UpdateStatusByIdAsync)
                await _mongoRepo.UpdateStatusByIdAsync(newSubmission.Id, newSubmission.Status);

                //// Update u ReliableDictionary
                using (var tx = StateManager.CreateTransaction())
                {
                    await submissions.SetAsync(tx, newSubmission.Id, newSubmission);
                    await tx.CommitAsync();
                }

                // ✅ 5. Pošalji SignalR obavještenje preko WebApi
                try
                {
                    var httpClient = new HttpClient();
                    var notification = new StatusChangeNotificationDto
                    {
                        WorkId = newSubmission.Id,
                        NewStatus = newSubmission.Status,
                        Title = newSubmission.Title,
                        EstimatedAnalysisTime = newSubmission.EstimatedAnalysisTime,
                        SubmittedAt = newSubmission.SubmittedAt,
                        StudentName = newSubmission.StudentName

                    };


                    await httpClient.PostAsJsonAsync("http://localhost:8285/api/submission/notify-status-change", notification);
                }
                catch (Exception ex)
                {
                    // Možeš logovati da SignalR notifikacija nije prošla, ali NE bacaj exception
                    ServiceEventSource.Current.ServiceMessage(this.Context, "SignalR notification failed: " + ex.Message);
                }

                return OperationResult<Guid>.Ok(newSubmission.Id);
            }
            catch (Exception ex)
            {
                await _mongoRepo.DeleteByIdAsync(newSubmission.Id); // ako postoji ta metoda
                return OperationResult<Guid>.Fail("Error saving submission: " + ex.Message);
            }
        }


        public async Task<List<SubmittedWork>> GetAllSubmissionsAsync() //
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
        public async Task<List<SubmittedWork>> GetWorksByStudentIdAsync(Guid studentId) // Testirano - Dobavljanje svih radova na osnovu ID studenta - Student
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
                // Prvo obriši sve postojeće zapise u ReliableDictionary
                var allKeys = await submissionsDict.CreateEnumerableAsync(tx, EnumerationMode.Unordered);
                var enumerator = allKeys.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    await submissionsDict.TryRemoveAsync(tx, enumerator.Current.Key);
                }

                // Zatim učitaj sve iz Mongo i upiši u ReliableDictionary
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


        public async Task<List<SubmittedWork>> GetSubmissionsByStatusAsync(WorkStatus status)
        {
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, SubmittedWork>>("submissions");
            var result = new List<SubmittedWork>();

            using (var tx = StateManager.CreateTransaction())
            {
                var enumerable = await dict.CreateEnumerableAsync(tx);
                var enumerator = enumerable.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    var work = enumerator.Current.Value;
                    if (work.Status == status)
                    {
                        result.Add(work);
                    }
                }
            }

            return result;
        }

        private TimeSpan EstimateAnalysisTime(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return TimeSpan.FromMinutes(1); // fallback

            var charCount = content.Length;

            if (charCount < 1000)
                return TimeSpan.FromMinutes(1);
            if (charCount < 3000)
                return TimeSpan.FromMinutes(2);
            if (charCount < 6000)
                return TimeSpan.FromMinutes(3);

            return TimeSpan.FromMinutes(5); // veći fajlovi
        }


    }
}