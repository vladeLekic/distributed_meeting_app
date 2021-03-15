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
using Microsoft.VisualBasic.CompilerServices;

namespace MeetingData
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class MeetingData : StatefulService, IMeetingDataInterface
    {
        public MeetingData(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<long> GetLoad()
        {
            IReliableDictionary<string, long> dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("serviceData");
            long ret = 0;
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {

                ConditionalValue<long> cond = await dictionary.TryGetValueAsync(tx, "PreviousRequestCounter");

                if (cond.HasValue)
                    ret = cond.Value;
                else
                    ret = 0;

                await tx.CommitAsync();
            }

            return ret;
        }

        public async Task<Meeting> GetMeeting(string identificator, string owner)
        {
            IReliableDictionary<string, long> iterationsDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("serviceData");

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await iterationsDictionary.AddOrUpdateAsync(tx, "RequestCounter", 1, (key, value) => ++value);

                await tx.CommitAsync();
            }

            IReliableDictionary<string, Dictionary<string, Meeting>> meetingDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Dictionary<string, Meeting>>>("meetingDictionary");
            Meeting ret = null;

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalValue<Dictionary<string, Meeting>> result = await meetingDictionary.TryGetValueAsync(tx, owner);

                if (result.HasValue)
                {
                    bool b = result.Value.TryGetValue(identificator, out ret);
                }

                await tx.CommitAsync();
            }

            return ret;
        }

        public async Task<string> AddMeeting(string identificator, MeetingJSON meetingJSON)
        {
            IReliableDictionary<string, long> iterationsDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("serviceData");
            IReliableDictionary<string, Dictionary<string, Meeting>> meetingDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Dictionary<string, Meeting>>>("meetingDictionary");

            Meeting meeting = new Meeting(meetingJSON, identificator);

            foreach (string attender in meetingJSON.Attenders)
            {
                int partitionKey = HashBase(attender) % 8;

                IUserDataInterface proxy = GetStetefulUserDataServiceProxy(partitionKey);

                await proxy.AddNewMeeting(attender, identificator, meeting.Owner);

                meeting.Attenders.Add(attender, true);
            }

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await iterationsDictionary.AddOrUpdateAsync(tx, "RequestCounter", 1, (key, value) => ++value);

                await meetingDictionary.AddOrUpdateAsync(tx, meetingJSON.Owner, new Dictionary<string, Meeting>(), (key, value) =>
                {
                    if(!value.ContainsKey(identificator))
                    {
                        value.Add(identificator, meeting);
                        return value;
                    }
                    else
                    {
                        throw new AggregateException();
                    }
                });

                await tx.CommitAsync();
            }

            return identificator;
        }

        public async Task<bool> SetRequestCoefficient(double requestCoefficient)
        {
            IReliableDictionary<string, double> dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, double>>("serviceCoeffData");
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await dictionary.AddOrUpdateAsync(tx, "RequestCoefficient", requestCoefficient, (key, value) => requestCoefficient);
                
                await tx.CommitAsync();
            }

            return true;
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

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            IReliableDictionary<string, long> dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("serviceData");
            IReliableDictionary<string, double> coeffDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, double>>("serviceCoeffData");

            double requestCoefficient = 10;
            long requestCounter = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    ConditionalValue<double> conditionalValue = await coeffDictionary.TryGetValueAsync(tx, "RequestCoefficient");

                    if (conditionalValue.HasValue)
                    {
                        requestCoefficient = conditionalValue.Value * 10;
                    }
                    else
                    {
                        requestCoefficient = 10;
                    }

                    await tx.CommitAsync();
                }


                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    ConditionalValue<long> cond = await dictionary.TryGetValueAsync(tx, "RequestCounter");
                    if (cond.HasValue)
                        requestCounter = cond.Value;
                    else
                        requestCounter = 0;

                    await dictionary.SetAsync(tx, "RequestCounter", 0);
                    await dictionary.SetAsync(tx, "PreviousRequestCounter", requestCounter);

                    await tx.CommitAsync();
                }

                this.Partition.ReportLoad(new List<LoadMetric> { new LoadMetric("RequestCount", (int)(requestCounter * requestCoefficient)) });

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}
