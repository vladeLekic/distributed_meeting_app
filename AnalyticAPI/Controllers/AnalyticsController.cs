using Communication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnalyticAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(ILogger<AnalyticsController> logger)
        {
            _logger = logger;
        }

        [HttpGet("getLoadReport")]
        public async Task<string> GetLoadReport()
        {
            IAnalyticsInterface proxy = ServiceProxy.Create<IAnalyticsInterface>(
                   new Uri("fabric:/OnBoardingApplication/AnalyticService"),
                   new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(0));

            return await proxy.GetAnalyticsMessage();
        }

        [HttpGet("geResponseTimeReport")]
        public async Task<string> GetResponseTimeReport()
        {
            IAnalyticsInterface proxy = ServiceProxy.Create<IAnalyticsInterface>(
                   new Uri("fabric:/OnBoardingApplication/AnalyticService"),
                   new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(0));

            return await proxy.GetResponseTimeReport();
        }
    }
}
