using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Communication
{
    public interface IMeetingOrchestratorInterface: IService
    {
        Task<string> AddMeeting(MeetingJSON meetingJSON);

        Task<Meeting> GetMeeting(string identificator, string owner);
    }
}
