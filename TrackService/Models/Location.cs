using System.Collections.Generic;

namespace TrackService.Models
{
    public class Feeds
    {
        public List<Location> SendLocation { get; set; }
    }
    public class Location
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Timestamp { get; set; }
        public int DeviceId { get; set; }
    }
}
