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
using System.Text;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Data;

namespace AnalyticService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class AnalyticService : StatefulService, IAnalyticsInterface
    {
        public AnalyticService(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<string> GetAnalyticsMessage()
        {
            StringBuilder sb = new StringBuilder();


            for (int partitionKey = 0; partitionKey < 16; partitionKey++)
            {
                IMeetingDataInterface proxy = ServiceProxy.Create<IMeetingDataInterface>(
                    new Uri("fabric:/OnBoardingApplication/MeetingData"),
                    new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(partitionKey));

                long requests = await proxy.GetLoad();
                sb.Append("p" + partitionKey + " [" + requests + "]\n");
            }

            return sb.ToString();
        }

        public async Task<string> GetResponseTimeReport()
        {
            IReliableDictionary<long, AnalyticData> dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, AnalyticData>>("analyticData");
            StringBuilder stringBuilder = new StringBuilder();
            List<double> avgResponseTime = new List<double>();
            double mean = 0, standardDeviation = 0;
            long numberOfActivePartitions = 0;

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                for (int partition = 0; partition < 16; partition++)
                {
                    ConditionalValue<AnalyticData> conditionalValue = await dictionary.TryGetValueAsync(tx, partition);

                    double msPerRequest = 0;
                    long numberOfRequests = 0;

                    if (conditionalValue.HasValue)
                    {
                        numberOfRequests = conditionalValue.Value.Requests;
                        msPerRequest = conditionalValue.Value.ResponseTime / numberOfRequests;

                        mean += msPerRequest;
                        numberOfActivePartitions++;
                    }

                    avgResponseTime.Add(msPerRequest);
                    stringBuilder.Append("p" + partition + " [" + msPerRequest + " ms/request, " + numberOfRequests + " requests]\n");
                }
            }

            if (numberOfActivePartitions > 1)
            {
                mean /= numberOfActivePartitions;

                for (int partition = 0; partition < 16; partition++)
                {
                    if (avgResponseTime[partition] != 0)
                    {
                        standardDeviation += Math.Pow(avgResponseTime[partition] - mean, 2);
                    }
                }

                standardDeviation = Math.Sqrt(standardDeviation / (numberOfActivePartitions - 1));

                stringBuilder.Append("Standard deviation [" + standardDeviation + "]\n");
                stringBuilder.Append("Arithmetic mean [" + mean + "]\n");
            }

            return stringBuilder.ToString();
        }

        public async Task<Boolean> SendResponseTimeReport(long PartitionId, long Requests, double ResponseTime)
        {
            try
            {
                IReliableDictionary<long, AnalyticData> dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, AnalyticData>>("analyticData");

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    await dictionary.AddOrUpdateAsync(tx, PartitionId, new AnalyticData { Requests = Requests, ResponseTime = ResponseTime }, (key, value) =>
                        new AnalyticData { Requests = Requests + value.Requests, ResponseTime = ResponseTime + value.ResponseTime });

                    await tx.CommitAsync();
                }

                return true;
            }
            catch (AggregateException)
            {
                return false;
            }
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            IReliableDictionary<long, AnalyticData> dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, AnalyticData>>("analyticData");

            // wait for initial analytics info
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<double> responseTimes = new List<double>();
                double activePartitionTimes = 0;
                int numberOfActivePartitions = 0;


                using (ITransaction tx = this.StateManager.CreateTransaction())
                {

                    for(int partitionID = 0; partitionID < 16; partitionID++)
                    {
                        ConditionalValue<AnalyticData> conditionalValue = await dictionary.TryGetValueAsync(tx, partitionID);

                        if (conditionalValue.HasValue && conditionalValue.Value.Requests !=0)
                        {
                            responseTimes.Add(conditionalValue.Value.ResponseTime / conditionalValue.Value.Requests);
                            numberOfActivePartitions++;
                            activePartitionTimes += conditionalValue.Value.ResponseTime / conditionalValue.Value.Requests;
                        }
                        else
                        {
                            responseTimes.Add(0);
                        }
                    }
                    
                    await tx.CommitAsync();
                }

                double meanTime = activePartitionTimes / numberOfActivePartitions;

                for (int partitionID = 0; partitionID < 16; partitionID++)
                {
                    if(responseTimes[partitionID] != 0)
                    {
                        try
                        {
                            IMeetingDataInterface proxy = ServiceProxy.Create<IMeetingDataInterface>(
                                new Uri("fabric:/OnBoardingApplication/MeetingData"),
                                new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(partitionID));

                            await proxy.SetRequestCoefficient(meanTime / responseTimes[partitionID]);
                        }
                        catch (AggregateException)
                        {

                        }
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }
}
