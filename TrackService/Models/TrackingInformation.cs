using Newtonsoft.Json;
using System;

namespace TrackService.Models
{
    public class mobiles
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public int vehicleId { get; set; }
        public int institutionId { get; set; }
        public DateTime timestamp { get; set; }
        public bool isLive { get; set; }

    }

    public class coordinates
    {
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
        public string timestamp { get; set; }
        public string mobileId { get; set; }
        public int deviceId { get; set; }
    }
}
