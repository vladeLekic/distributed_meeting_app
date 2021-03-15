using Communication;
using System;
using System.Collections.Generic;
using System.Text;

namespace WorkerService
{
    class WorkerData
    {
        public User User { get; set; }

        public long Requests { get; set; }

        public double ResponseTime { get; set; }
    }
}
