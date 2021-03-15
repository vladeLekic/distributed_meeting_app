using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Communication
{
    public interface IMeetingDataInterface: IService
    {
        Task<string> AddMeeting(string Identificator, MeetingJSON meetingJSON);

        Task<Meeting> GetMeeting(string Identificator, string owner);

        Task<long> GetLoad();

        Task<bool> SetRequestCoefficient(double requestCoefficient);
    }
}
