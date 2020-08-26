
using Newtonsoft.Json;
using RethinkDb.Driver.Ast;
using System;
using System.Collections.Generic;

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
