using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Communication
{
    public interface IAnalyticsInterface: IService
    {
        Task<string> GetAnalyticsMessage();

        Task<string> GetResponseTimeReport();

        Task<Boolean> SendResponseTimeReport(long PartitionID, long Requests, double ResponseTime);
    }
}
