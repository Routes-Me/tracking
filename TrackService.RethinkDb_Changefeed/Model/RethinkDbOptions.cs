using System;
using System.Collections.Generic;
using System.Text;

namespace TrackService.RethinkDb_Changefeed.Model
{
    public class RethinkDbOptions
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Database { get; set; }
        public int Timeout { get; set; }
    }
}
