using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Communication
{
    [DataContract]
    public class MeetingCredentials
    {
        [DataMember]
        public string Identificator { get; set; }

        [DataMember]
        public string Owner { get; set; }
    }
}
