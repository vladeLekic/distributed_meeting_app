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
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Data;

namespace MeetingOrchestrator
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class MeetingOrchestrator : StatefulService, IMeetingOrchestratorInterface
    {
        public MeetingOrchestrator(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<Meeting> GetMeeting(string identificator, string owner)
        {   
            IUserDataInterface userDataProxy = GetStetefulUserDataServiceProxy(owner);
            User user = await userDataProxy.GetUser(owner);

            if (user == null) return null;

            IReliableDictionary<string, long> dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("serviceData");
            
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await dictionary.AddOrUpdateAsync(tx, "IterationCounter", 1, (key, value) => ++value);

                await tx.CommitAsync();
            }

            IMeetingDataInterface meetingDataProxy = GetStetefulMeetingDataServiceProxy(user.DataPartition);

            return await meetingDataProxy.GetMeeting(identificator, owner);
        }

        public async Task<string> AddMeeting(MeetingJSON meetingJSON)
        {
            //var fabricClient = new FabricClient();
            //var partitions = await fabricClient.QueryManager.GetPartitionListAsync(new Uri("fabric:/OnBoardingApplication/MeetingData"));
            //int partitionKey = HashCode(owner, partitions.Count);

            meetingJSON.Attenders.Add(meetingJSON.Owner);

            IReliableDictionary<string, long> dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("serviceData");
            long counter;

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalValue<long> result = await dictionary.TryGetValueAsync(tx, "TotalCounter");

                if (result.HasValue) counter = result.Value;
                else counter = 0;

                await dictionary.SetAsync(tx, "TotalCounter", counter + 1);

                await tx.CommitAsync();
            }

            string identificator = "m" + counter;
            
            //IUserDataInterface userDataProxy = GetStetefulUserDataServiceProxy(meetingJSON.Owner);
            
            int dataPartition = HashBase(meetingJSON.Owner) % 16;

            IMeetingDataInterface meetingDataProxy = GetStetefulMeetingDataServiceProxy(dataPartition);

            return await meetingDataProxy.AddMeeting(identificator, meetingJSON);
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

        private int HashCode(string owner, int numberOfPartitions)
        {
            int hashCode = 0;

            for (int i = 0; i < owner.Length; i++)
            {
                hashCode += owner[i];
            }
            
            return hashCode % numberOfPartitions;
        }

        private IMeetingDataInterface GetStetefulMeetingDataServiceProxy(int partitionKey)
        {
            return ServiceProxy.Create<IMeetingDataInterface>(
               new Uri("fabric:/OnBoardingApplication/MeetingData"),
               new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(partitionKey));
        }

        private IUserDataInterface GetStetefulUserDataServiceProxy(string Username)
        {
            return ServiceProxy.Create<IUserDataInterface>(
               new Uri("fabric:/OnBoardingApplication/UserData"),
               new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(HashBase(Username) % 8));
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
    }
}
