using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrackService.Models
{
    public class CommonMessage
    {
        public static string NotAllowed = "You are not allowed to subscribe.";
        public static string InstitutionNotFound = "Institution does not exists.";
        public static string BadRequestForInstitution = "Bad request value. Invalid InstitutionId!";
        public static string VehicleNotFound = "Vehicle does not exists.";
        public static string BadRequestForVehicle = "Bad request value. Invalid VehicleId!";
    }
}
