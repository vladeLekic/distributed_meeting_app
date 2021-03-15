using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Text;

namespace Communication
{
    [DataContract]
    public class Meeting
    {

        public Meeting(MeetingJSON meetingJSON, string Identificator)
        {
            this.Identificator = Identificator;
            this.RepetitionPeriod = meetingJSON.RepetitionPeriod;
            this.Duration = meetingJSON.Duration;
            this.StartDate =  new DateTime(meetingJSON.Year, meetingJSON.Month, meetingJSON.Day, meetingJSON.Hours, meetingJSON.Minutes, 0);
            this.Title = meetingJSON.Title;
            this.Owner = meetingJSON.Owner;
            this.Description = meetingJSON.Description;
            Attenders = new Dictionary<string, bool>();
        }

        [DataMember]
        public string Owner { get; set; }

        [DataMember]
        public Dictionary<string, bool> Attenders { get; set; }
 
        [DataMember]
        public string Identificator { get; set; }

        [DataMember]
        public int RepetitionPeriod { get; set; }

        [DataMember]
        public int Duration { get; set; }

        [DataMember]
        public DateTime StartDate { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Description { get; set; }
    }
}
