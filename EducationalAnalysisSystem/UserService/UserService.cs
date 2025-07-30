using Common.Configurations;
using Common.DTOs;
using Common.Helpers;
using Common.Interfaces;
using Common.Models;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Fabric;
using UserService.Data;

namespace UserService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class UserService : StatefulService, IUserService
    {
        private readonly UserMongoRepository _mongoRepo;

        public UserService(StatefulServiceContext context) : base(context)
        {
            var settings = new UserDbSettings
            {
                ConnectionString = "mongodb://localhost:27017",
                DatabaseName = "EducationalSystemDb",
                CollectionName = "Users"
            };

            _mongoRepo = new UserMongoRepository(settings);
        }

        public async Task<OperationResult<Guid>> RegisterUserAsync(RegisterRequest request)
        {
            var users = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, User>>("users");

            using (var tx = StateManager.CreateTransaction())
            {
                var allUsers = await users.CreateEnumerableAsync(tx);
                var enumerator = allUsers.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    if (enumerator.Current.Value.Username == request.Username)
                    {
                        return OperationResult<Guid>.Fail("Username already exists.");
                    }
                }

                var newUser = new User
                {
                    Username = request.Username,
                    Password = PasswordHasher.Hash(request.Password),
                    Role = request.Role
                };

                await _mongoRepo.InsertUserAsync(newUser);


                await users.AddAsync(tx, newUser.Id, newUser);
                await tx.CommitAsync();

                return OperationResult<Guid>.Ok(newUser.Id);
            }
        }

        private async Task LoadUsersAsync()
        {
            var usersDict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, User>>("users");

            // 1. Uzimamo sve korisnike iz MongoDB
            var allUsers = await _mongoRepo.GetAllAsync();

            using (var tx = StateManager.CreateTransaction())
            {
                // 2. Brišemo sve postojeće iz ReliableDictionary
                var enumerable = await usersDict.CreateEnumerableAsync(tx, EnumerationMode.Unordered);
                using (var e = enumerable.GetAsyncEnumerator())
                {
                    while (await e.MoveNextAsync(CancellationToken.None))
                    {
                        await usersDict.TryRemoveAsync(tx, e.Current.Key);
                    }
                }

                // 3. Ubacujemo sve korisnike iz baze
                foreach (var user in allUsers)
                {
                    await usersDict.SetAsync(tx, user.Id, user);
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

            await LoadUsersAsync(); // učitavanje korisnika iz Mongo u ReliableDictionary

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

        public async Task<User?> LoginAsync(LoginRequest request)
        {
            var users = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, User>>("users");

            using (var tx = StateManager.CreateTransaction())
            {
                var allUsers = await users.CreateEnumerableAsync(tx);
                var enumerator = allUsers.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    var user = enumerator.Current.Value;
                    var hashed = PasswordHasher.Hash(request.Password);

                    if (user.Username == request.Username && user.Password == hashed)
                    {
                        return user;
                    }
                }

                return null;
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var dict = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, User>>("users");
            var result = new List<User>();

            using (var tx = StateManager.CreateTransaction())
            {
                var enumerable = await dict.CreateEnumerableAsync(tx);
                var enumerator = enumerable.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    result.Add(enumerator.Current.Value);
                }
            }

            return result;
        }
        public async Task<OperationResult<Guid>> CreateUserAsync(CreateUserRequest request)
        {
            var users = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, User>>("users");

            using (var tx = StateManager.CreateTransaction())
            {
                var allUsers = await users.CreateEnumerableAsync(tx);
                var enumerator = allUsers.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    if (enumerator.Current.Value.Username == request.Username)
                    {
                        return OperationResult<Guid>.Fail("Username already exists.");
                    }
                }

                var newUser = new User
                {
                    Username = request.Username,
                    Password = PasswordHasher.Hash(request.Password),
                    Role = request.Role
                };

                await _mongoRepo.InsertUserAsync(newUser);
                await users.AddAsync(tx, newUser.Id, newUser);
                await tx.CommitAsync();

                return OperationResult<Guid>.Ok(newUser.Id);
            }
        }
        public async Task<OperationResult<bool>> DeleteUserAsync(Guid userId)
        {
            var users = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, User>>("users");

            using (var tx = StateManager.CreateTransaction())
            {
                var exists = await users.ContainsKeyAsync(tx, userId);
                if (!exists)
                    return OperationResult<bool>.Fail("User not found.");

                await users.TryRemoveAsync(tx, userId);
                await _mongoRepo.DeleteByIdAsync(userId);
                await tx.CommitAsync();

                return OperationResult<bool>.Ok(true);
            }
        }

        public async Task<OperationResult<bool>> UpdateUserAsync(Guid userId, UpdateUserRequest request)
        {
            var users = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, User>>("users");

            using (var tx = StateManager.CreateTransaction())
            {
                var result = await users.TryGetValueAsync(tx, userId);
                if (!result.HasValue)
                    return OperationResult<bool>.Fail("User not found.");

                var user = result.Value;

                if (!string.IsNullOrEmpty(request.NewPassword))
                {
                    user.Password = PasswordHasher.Hash(request.NewPassword);
                }

                if (request.NewRole.HasValue)
                {
                    user.Role = request.NewRole.Value;
                }

                await users.SetAsync(tx, userId, user);
                await tx.CommitAsync();

                await _mongoRepo.UpdateAsync(user); // vidi dole

                return OperationResult<bool>.Ok(true);
            }
        }

        public async Task<OperationResult<bool>> SetMaxSubmissionsAsync(int max)
        {
            var settingsDict = await StateManager.GetOrAddAsync<IReliableDictionary<string, int>>("systemSettings");

            using (var tx = StateManager.CreateTransaction())
            {
                await settingsDict.SetAsync(tx, "MaxSubmissionsPerStudent", max);
                await tx.CommitAsync();
                return OperationResult<bool>.Ok(true);
            }
        }

        public async Task<int?> GetMaxSubmissionsAsync()
        {
            var settingsDict = await StateManager.GetOrAddAsync<IReliableDictionary<string, int>>("systemSettings");

            using (var tx = StateManager.CreateTransaction())
            {
                var result = await settingsDict.TryGetValueAsync(tx, "MaxSubmissionsPerStudent");
                return result.HasValue ? result.Value : null;
            }
        }


    }
}
