using System;
using System.Collections.Generic;
using System.Text;

namespace TrackService.RethinkDb_Changefeed.Model.Common
{
    public class AppSettings
    {
        public string Host { get; set; }
        public string AccessSecretKey { get; set; }
        public string SessionTokenIssuer { get; set; }
        public string DashboardAudience { get; set; }
        public string RoutesAppAudience { get; set; }
        public string ScreenAudience { get; set; }
        public string BusValidatorAudience { get; set; }
    }
}
