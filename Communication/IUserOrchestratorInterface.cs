using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Communication
{
    public interface IUserOrchestratorInterface: IService
    {
        Task<string> AddUser(UserJSON userJSON);

        Task<User> GetUser(string Username);

        Task<User> UpdateUser(string Username, string Password);

        Task<List<User>> GetAllUsers();

        Task<List<MeetingCredentials>> GetMeetingsForUser(string Username);
    }
}
