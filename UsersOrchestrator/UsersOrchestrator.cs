using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Communication;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace UsersOrchestrator
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class UsersOrchestrator : StatelessService, IUserOrchestratorInterface
    {
        public UsersOrchestrator(StatelessServiceContext context)
            : base(context)
        { }

        public async Task<string> AddUser(UserJSON userJSON)
        {
            int hashBase = HashBase(userJSON.Username);
            int partitionKey = hashBase % 8;
            int dataPartitionKey = hashBase % 16;

            IUserDataInterface proxy = GetStetefulUserDataServiceProxy(partitionKey);

            return await proxy.AddUser(userJSON, dataPartitionKey);
        }

        public async Task<List<MeetingCredentials>> GetMeetingsForUser(string Username)
        {
            int hashBase = HashBase(Username);
            int partitionKey = hashBase % 8;

            IUserDataInterface proxy = GetStetefulUserDataServiceProxy(partitionKey);

            return await proxy.GetMeetingsForUser(Username);
        }

        public async Task<User> GetUser(string Username)
        {
            int partitionKey = HashBase(Username) % 8;

            IUserDataInterface proxy = GetStetefulUserDataServiceProxy(partitionKey);

            return await proxy.GetUser(Username);
        }

        public async Task<User> UpdateUser(string Username, string Password)
        {
            int partitionKey = HashBase(Username) % 8;

            IUserDataInterface proxy = GetStetefulUserDataServiceProxy(partitionKey);

            return await proxy.UpdateUser(Username, Password);
        }

        public async Task<List<User>> GetAllUsers()
        {
            List<User> allUsers = new List<User>();

            for (int partitionKey = 0; partitionKey < 8; partitionKey++)
            {
                IUserDataInterface proxy = GetStetefulUserDataServiceProxy(partitionKey);

                List<User> partitionUsers = await proxy.GetAllUsers();

                allUsers.AddRange(partitionUsers);
            }

            return allUsers;
        }

        private int HashBase(string Username)
        {
            int hashBase = 0;

            for (int i = 0; i < Username.Length; i++)
            {
                hashBase += Username[i];
            }

            return hashBase;
        }

        private IUserDataInterface GetStetefulUserDataServiceProxy(int partitionKey)
        {
            return ServiceProxy.Create<IUserDataInterface>(
               new Uri("fabric:/OnBoardingApplication/UserData"),
               new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(partitionKey));
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }
    }
}
