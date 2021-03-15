using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Communication;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace UserData
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class UserData : StatefulService, IUserDataInterface
    {
        public UserData(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<string> AddUser(UserJSON userJSON, int DataPartition)
        {
            IReliableDictionary<string, User> userDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, User>>("userDictionary");
            string ret = "OK";
            User user = new User(userJSON, DataPartition);

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalValue<User> result = await userDictionary.TryGetValueAsync(tx, user.Username);

                if (!result.HasValue)
                {
                    await userDictionary.AddAsync(tx, user.Username, user);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }
                else
                {
                    ret = "Error: Username isn't free";
                }
            }

            return ret;
        }

        public async Task<List<User>> GetAllUsers()
        {
            IReliableDictionary<string, User> userDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, User>>("userDictionary");

            List<User> partitionUsers = new List<User>();

            CancellationToken ct = new CancellationToken();

            using (ITransaction tx = StateManager.CreateTransaction())
            {
                var list = await userDictionary.CreateEnumerableAsync(tx);

                var enumerator = list.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(ct))
                {
                    partitionUsers.Add(enumerator.Current.Value);
                }
                return partitionUsers;
            }
        }

        public async Task<List<MeetingCredentials>> GetMeetingsForUser(string Username)
        {
            List<MeetingCredentials> list = new List<MeetingCredentials>();

            IMeetingOrchestratorInterface proxy = ServiceProxy.Create<IMeetingOrchestratorInterface>(
               new Uri("fabric:/OnBoardingApplication/MeetingOrchestrator"),
               new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(0));

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                User user = await GetUser(Username);

                if (user == null) return list;

                List<KeyValuePair<string, List<string>>> owners = user.meetings.ToList();

                foreach (KeyValuePair<string, List<string>> owner in owners)
                {
                    foreach (string meetingID in owner.Value)
                    {
                        // Meeting meeting = await proxy.GetMeeting(meetingID, owner.Key);

                        list.Add(new MeetingCredentials { Identificator = meetingID, Owner = owner.Key});
                    }
                }

                await tx.CommitAsync();
            }

            return list;
        }

        public async Task<User> AddNewMeeting(string Username, string Identificator, string Owner)
        {
            IReliableDictionary<string, User> userDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, User>>("userDictionary");
            User user = null;
            List<string> list;

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalValue<User> result = await userDictionary.TryGetValueAsync(tx, Username);

                if (result.HasValue)
                {
                    user = result.Value;

                    bool exist = user.meetings.TryGetValue(Owner, out list);

                    if (!exist)
                    {
                        list = new List<string>();
                        user.meetings.Add(Owner, list);
                    }

                    list.Add(Identificator);

                    await userDictionary.AddOrUpdateAsync(tx, Username, user, (key, oldUser) => user);

                    await tx.CommitAsync();
                }
            }

            return user;
        }

        public async Task<User> UpdateUser(string Username, string Password)
        {
            IReliableDictionary<string, User> userDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, User>>("userDictionary");
            User user = null;

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalValue<User> result = await userDictionary.TryGetValueAsync(tx, Username);

                if (result.HasValue)
                {
                    user = result.Value;
                    user.Password = Password;

                    await userDictionary.AddOrUpdateAsync(tx, Username, user, (key, oldUser) => user);

                    await tx.CommitAsync();
                }
            }

            return user;
        }

        public async Task<User> GetUser(string Username)
        {
            IReliableDictionary<string, User> userDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, User>>("userDictionary");
            User ret = null;

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                var result = await userDictionary.TryGetValueAsync(tx, Username);

                if (result.HasValue)
                {
                    ret = result.Value;
                }

                await tx.CommitAsync();
            }

            return ret;
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
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
