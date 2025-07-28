using Common.DTOs;
using Common.Interfaces;
using Common.Models;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Fabric;

namespace UserService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class UserService : StatefulService, IUserService
    {

        public async Task<Guid> RegisterUserAsync(RegisterRequest request)
        {
            var users = await StateManager.GetOrAddAsync<IReliableDictionary<Guid, User>>("users");

            using (var tx = StateManager.CreateTransaction())
            {
                // Provjeri da li već postoji korisnik sa tim username-om
                var allUsers = await users.CreateEnumerableAsync(tx);
                var enumerator = allUsers.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    if (enumerator.Current.Value.Username == request.Username)
                    {
                        throw new InvalidOperationException("Username already exists.");
                    }
                }

                var newUser = new User
                {
                    Username = request.Username,
                    Password = request.Password, // za sad plain tekst
                    Role = request.Role
                };

                await users.AddAsync(tx, newUser.Id, newUser);
                await tx.CommitAsync();

                return newUser.Id;
            }
        }


        public UserService(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[0];
        }

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
                    if (user.Username == request.Username && user.Password == request.Password)
                    {
                        return user;
                    }
                }

                return null;
            }
        }

    }
}
