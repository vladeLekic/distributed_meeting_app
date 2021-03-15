using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Communication
{
    [DataContract]
    public class User
    {
        public User(UserJSON userJSON, int DataPartition)
        {
            Username = userJSON.Username;
            Password = userJSON.Password;
            Age = userJSON.Age;
            Firstname = userJSON.Firstname;
            Lastname = userJSON.Lastname;
            this.DataPartition = DataPartition;
            meetings = new Dictionary<string, List<string>>();
        }

        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public string Password { get; set; }

        [DataMember]
        public string Firstname { get; set; }

        [DataMember]
        public string Lastname { get; set; }

        [DataMember]
        public int Age { get; set; }

        [DataMember]
        public int DataPartition { get; set; }

        [DataMember]
        public Dictionary<string, List<string>> meetings;
    }
}
