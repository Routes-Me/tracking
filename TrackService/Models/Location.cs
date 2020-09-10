using System.Collections.Generic;

namespace TrackService.Models
{
    public class LocationFeed
    { 
        public List<SendLocation> SendLocation { get; set; }
    }
    public class SendLocation
    {
        public string latitude { get; set; }
        public string longitude { get; set; }
        public string timestamp { get; set; }
        public int deviceId { get; set; }
    }
}
