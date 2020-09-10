using System;
using System.Collections.Generic;

namespace TrackService.RethinkDb_Abstractions
{
    public class MobilesModel
    {
        public int vehicleId { get; set; }
        public int institutionId { get; set; }
        public string timeStamp { get; set; }
        

        public override string ToString()
        {
            return $"vehicleId: {vehicleId}, institutionId: {institutionId}, timeStamp: {timeStamp}";
        }
    }

    public class CordinatesModel
    {
        public string mobileId { get; set; }
        public int deviceId { get; set; }
        public double longitude { get; set; }
        public double latitude { get; set; }
        public string timeStamp { get; set; }


        public override string ToString()
        {
            return $"mobileId: {mobileId}, deviceId: {deviceId}, latitude: {latitude}, longitude: {longitude}, timeStamp: {timeStamp}";
        }
    }

    public class Mobiles
    {
        public int vehicleId { get; set; }
        public int institutionId { get; set; }
        public DateTime timeStamp { get; set; }
        public bool isLive { get; set; }

        public override string ToString()
        {
            return $"vehicleId: {vehicleId}, institutionId: {institutionId}, timeStamp: {timeStamp}, isLive: {isLive}";
        }
    }

    public class Coordinates
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public DateTime timeStamp { get; set; }
        public string mobileId { get; set; }
        public int deviceId { get; set; }
        public override string ToString()
        {
            return $"mobileId: {mobileId}, latitude: {latitude}, longitude: {longitude}, timeStamp: {timeStamp}, deviceId: {deviceId}";
        }
    }

    public class IdleModel
    {
        public string status { get; set; }
        public string institutionId { get; set; }
        public string startAt { get; set; }
        public string endAt { get; set; }

    }

    public class VehicleDetails
    {
        public string deviceId { get; set; }
        public string vehicleId { get; set; }
        public string institutionId { get; set; }
        public List<CoordinatesDetail> coordinates { get; set; }

    }

    public class CoordinatesDetail
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string timeStamp { get; set; }
    }

    public class ArchiveVehiclesList
    { 
        public List<ArchiveVehicles> archiveVehiclesList { get; set; }
    }
    public class ArchiveCoordinatesList
    {
        public List<ArchiveCoordinates> archiveCoordinatesList { get; set; }
    }
    public class DeleteVehicles
    {
        public double deviceId { get; set; }
        public string vehicleIds { get; set; }
        public string institutionIds { get; set; }
        public DateTime? start { get; set; }
        public DateTime? end { get; set; }
    }

    public class DeleteFeeds
    {
        public string vehicleIds { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
    }

    public class ArchiveVehicles
    {
        public string deviceId { get; set; }
        public int? vehicleId { get; set; }
        public int? institutionId { get; set; }
        public DateTime? timeStamp { get; set; }
        public int? isLive { get; set; }
    }


    public class ArchiveCoordinates
    {
        public string CoordinateId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Timestamp { get; set; }
        public int VehicleId { get; set; }
        public int DeviceId { get; set; }
        
        //public string mobileId { get; set; }
        //public double longitude { get; set; }
        //public double latitude { get; set; }
        //public DateTime? timeStamp { get; set; }
    }

    public class MobileJSONResponse
    {
        public string id { get; set;  }
        public int vehicleId { get; set; }
        public int institutionId { get; set; }
        public timeStamp timeStamp { get; set; }
        public bool isLive { get; set; }
    }

    public class timeStamp
    {
        public string reql_type { get; set; }
        public decimal epoch_time { get; set; }
        public string timezone { get; set; }
    }
}
