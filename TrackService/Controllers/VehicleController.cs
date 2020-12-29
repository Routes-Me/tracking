using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TrackService.Helper;
using TrackService.RethinkDb_Abstractions;
using IdleModel = TrackService.RethinkDb_Abstractions.IdleModel;
using VehicleDetails = TrackService.RethinkDb_Abstractions.VehicleDetails;

namespace TrackService.Controllers
{
    [ApiController]
    [Route("api")]
    public class VehicleController : ControllerBase
    {
        readonly static ConnectionMapping<string> _connections = new ConnectionMapping<string>();
        public readonly static AllVehicleMapping<string> _allvehicles = new AllVehicleMapping<string>();
        public readonly static InstitutionsMapping<string> _institutions = new InstitutionsMapping<string>();
        public readonly static VehiclesMapping<string> _subscribedvehicles = new VehiclesMapping<string>();
        private readonly ICoordinateChangeFeedbackBackgroundService _coordinateChangeFeedbackBackgroundService;
        public VehicleController(ICoordinateChangeFeedbackBackgroundService threadStatsChangefeedDbService)
        {
            _coordinateChangeFeedbackBackgroundService = threadStatsChangefeedDbService;
        }

        [HttpGet]
        [Route("tracking/vehicles/{vehicleId?}")]
        public IActionResult VehicleStatus(string vehicleId, [FromQuery] Pagination pageInfo, [FromQuery] IdleModel IdleModel)
        {
            dynamic response = _coordinateChangeFeedbackBackgroundService.GetVehicles(vehicleId, pageInfo, IdleModel);
            return StatusCode((int)response.statusCode, response);
        }
        [HttpDelete]
        [Route("tracking/vehicles/{vehicleId?}")]
        public IActionResult ClearLiveDatabase(string vehicleId)
        {
            dynamic response = _coordinateChangeFeedbackBackgroundService.ClearLiveTrackingDatabase(vehicleId);
            return StatusCode((int)response.statusCode, response);
        }
    }
}
