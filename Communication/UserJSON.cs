using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Communication
{
    [DataContract]
    public class UserJSON
    {
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
    }
}
