using Communication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        
        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpPost("addNewUser")]
        public async Task<string> AddNewUser([FromBody] UserJSON userJSON)
        {
            var statefulProxy = ServiceProxy.Create<IUserOrchestratorInterface>(
                  new Uri("fabric:/OnBoardingApplication/UsersOrchestrator"));

            return await statefulProxy.AddUser(userJSON);
        }

        [HttpGet("getUser")]
        public async Task<User> GetUser([FromQuery] string Username)
        {
            var statefulProxy = ServiceProxy.Create<IUserOrchestratorInterface>(
                           new Uri("fabric:/OnBoardingApplication/UsersOrchestrator"));

            return await statefulProxy.GetUser(Username);
        }

        [HttpGet("updateUser")]
        public async Task<User> UpdateUser([FromQuery] string Username, [FromQuery] string Password)
        {

            var statefulProxy = ServiceProxy.Create<IUserOrchestratorInterface>(
                           new Uri("fabric:/OnBoardingApplication/UsersOrchestrator"));

            return await statefulProxy.UpdateUser(Username, Password);
        }


        [HttpGet("getAllUsers")]
        public async Task<List<User>> GetAllUsers()
        {
            var statefulProxy = ServiceProxy.Create<IUserOrchestratorInterface>(
                           new Uri("fabric:/OnBoardingApplication/UsersOrchestrator"));

            return await statefulProxy.GetAllUsers();
        }

        [HttpPost("meeting/addMeeting")]
        public async Task<string> AddMeeting([FromBody] MeetingJSON meetingJSON)
        {
            var statefulProxy = ServiceProxy.Create<IMeetingOrchestratorInterface>(
                           new Uri("fabric:/OnBoardingApplication/MeetingOrchestrator"),
                           new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(0));

            return await statefulProxy.AddMeeting(meetingJSON);
        }

        [HttpGet("meeting/getMeeting")]
        public async Task<Meeting> GetMeeting([FromQuery] string identificator, [FromQuery] string owner)
        {
            var statefulProxy = ServiceProxy.Create<IMeetingOrchestratorInterface>(
                        new Uri("fabric:/OnBoardingApplication/MeetingOrchestrator"),
                        new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(0));

            return await statefulProxy.GetMeeting(identificator, owner);
        }

        [HttpGet("getMeetingsForUser")]
        public async Task<List<Meeting>> GetMeetingsForUser([FromQuery] string Username)
        {
            IUserOrchestratorInterface usersOrchestratorProxy = ServiceProxy.Create<IUserOrchestratorInterface>(
                        new Uri("fabric:/OnBoardingApplication/UsersOrchestrator"));

            IMeetingOrchestratorInterface meetingOrchestratorProxy = ServiceProxy.Create<IMeetingOrchestratorInterface>(
                        new Uri("fabric:/OnBoardingApplication/MeetingOrchestrator"),
                        new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(0));

            List<MeetingCredentials> meetingCredentials = await usersOrchestratorProxy.GetMeetingsForUser(Username);
            List<Meeting> meetings = new List<Meeting>();

            for(int i = 0; i < meetingCredentials.Count; i++)
            {
                Meeting meeting = await meetingOrchestratorProxy.GetMeeting(meetingCredentials[i].Identificator, meetingCredentials[i].Owner);

                if(meeting != null)
                {
                    meetings.Add(meeting);
                }
            }

            return meetings;
        }

        [HttpGet("getMeetingsCredentialsForUser")]
        public async Task<List<MeetingCredentials>> GetMeetingsCredentialsForUser([FromQuery] string Username)
        {
            IUserOrchestratorInterface usersOrchestratorProxy = ServiceProxy.Create<IUserOrchestratorInterface>(
                        new Uri("fabric:/OnBoardingApplication/UsersOrchestrator"));

            return await usersOrchestratorProxy.GetMeetingsForUser(Username);
        }
    }
}
