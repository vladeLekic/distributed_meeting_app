using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Communication
{
    public interface IUserDataInterface: IService
    { 
        Task<string> AddUser(UserJSON userJSON, int dataPartition);

        Task<User> AddNewMeeting(string Username, string Identificator, string Owner); 

        Task<User> GetUser(string Username);

        Task<User> UpdateUser(string Username, string Password);

        Task<List<User>> GetAllUsers();

        Task<List<MeetingCredentials>> GetMeetingsForUser(string Username);
    }
}
