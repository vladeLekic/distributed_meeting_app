using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace AnalyticService
{
    [DataContract]
    class AnalyticData
    {
        [DataMember]
        public long Requests { get; set; }

        [DataMember]
        public double ResponseTime { get; set; }
    }
}
