using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Communication
{
    [DataContract]
    public class MeetingJSON
    {
        [DataMember]
        public string Owner { get; set; }
        
        [DataMember]
        public List<string> Attenders { get; set; }
    
        [DataMember]
        public int RepetitionPeriod { get; set; }

        [DataMember]
        public int Duration { get; set; }

        [DataMember]
        public int Year { get; set; }

        [DataMember]
        public int Month { get; set; }

        [DataMember]
        public int Day { get; set; }

        [DataMember]
        public int Hours { get; set; }

        [DataMember]
        public int Minutes { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Description { get; set; }
    }
}
