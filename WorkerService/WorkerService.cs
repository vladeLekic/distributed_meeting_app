using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Communication;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Runtime;

namespace WorkerService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class WorkerService : StatelessService
    {
        public WorkerService(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            IUserOrchestratorInterface userStatelesslProxy = ServiceProxy.Create<IUserOrchestratorInterface>(
                            new Uri("fabric:/OnBoardingApplication/UsersOrchestrator"));

            IMeetingOrchestratorInterface meetingStatefulProxy = ServiceProxy.Create<IMeetingOrchestratorInterface>(
                        new Uri("fabric:/OnBoardingApplication/MeetingOrchestrator"),
                        new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(0));

            string response;

            const int numberOfUsers = 20;

            List<WorkerData> users = new List<WorkerData>();

            for (int i = 0; i < numberOfUsers; i++)
            {
                response = "";

                while (response != "OK")
                {
                    try
                    {
                        UserJSON userJSON = new UserJSON
                        {
                            Firstname = "User",
                            Lastname = "",
                            Username = "username" + new Random().Next(1, numberOfUsers * 10),
                            Password = "user123",
                            Age = 30
                        };

                        response = await userStatelesslProxy.AddUser(userJSON);

                        WorkerData workerData = new WorkerData
                        {
                            User = await userStatelesslProxy.GetUser(userJSON.Username),
                            Requests = 0,
                            ResponseTime = 0
                        };

                        users.Add(workerData);
                    }
                    catch (AggregateException)
                    {
                        await Task.Delay(1000);
                    }
                }
            }

            long iteration = 0;

            while (true)
            {
                for (int i = 0; i < numberOfUsers; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    MeetingJSON meetingJSON = new MeetingJSON
                    {
                        Owner = users[i].User.Username,
                        Attenders = new List<string>(),
                        RepetitionPeriod = -1,
                        Duration = 45,
                        Year = 2021,
                        Month = 3,
                        Day = 10,
                        Hours = 12,
                        Minutes = 0,
                        Title = "Generic meeting",
                        Description = "Generic event"
                    };

                    try
                    {
                        Stopwatch watch = new System.Diagnostics.Stopwatch();

                        watch.Start();

                        response = await meetingStatefulProxy.AddMeeting(meetingJSON);

                        watch.Stop();

                        users[i].Requests++;
                        users[i].ResponseTime += watch.ElapsedMilliseconds;
                    }
                    catch (AggregateException a)
                    {
                        ServiceEventSource.Current.ServiceMessage(this.Context, a.ToString());
                        await Task.Delay(1000);
                    }
                }

                try
                {
                    if (++iteration % 20 == 0)
                    {
                        iteration = 0;

                        IAnalyticsInterface proxy = ServiceProxy.Create<IAnalyticsInterface>(
                           new Uri("fabric:/OnBoardingApplication/AnalyticService"),
                           new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(0));

                        for (int i = 0; i < numberOfUsers; i++)
                        {
                            Boolean success = await proxy.SendResponseTimeReport(users[i].User.DataPartition, users[i].Requests, users[i].ResponseTime);

                            if (success)
                            {
                                users[i].Requests = 0;
                                users[i].ResponseTime = 0;
                            }
                        }
                    }
                }
                catch (AggregateException a)
                {
                    ServiceEventSource.Current.ServiceMessage(this.Context, a.ToString());
                    iteration = 20;
                }
            }
        }
    }
}
