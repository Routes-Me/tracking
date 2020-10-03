using System.Collections.Generic;

namespace TrackService.Models
{
    public class LocationFeed
    { 
        public List<SendLocation> SendLocation { get; set; }
    }
    public class SendLocation
    {
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
        public string timestamp { get; set; }
        public int deviceId { get; set; }
    }
}
