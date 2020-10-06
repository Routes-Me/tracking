using System;
using System.Collections.Generic;
using System.Text;

namespace TrackService.RethinkDb_Changefeed.Model.Common
{
    public class AppSettings
    {
        public string Secret { get; set; }
        public string ValidAudience { get; set; }
        public string ValidIssuer { get; set; }
        public string Host { get; set; }
    }
}
