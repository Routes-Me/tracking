using Newtonsoft.Json;
using System;

namespace TrackService.Models
{
    public class vehicles
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public int vehicleId { get; set; }
        public int institutionId { get; set; }
        public DateTime timeStamp { get; set; }
        public bool isLive { get; set; }

    }

    public class coordinates
    {
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
        public string timeStamp { get; set; }
        public string mobileId { get; set; }
        public int deviceId { get; set; }
    }
}
