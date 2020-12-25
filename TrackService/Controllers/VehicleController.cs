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
        [Route("vehicles/ideals")]
        public async Task<IActionResult> VehicleStatus([FromQuery] Pagination pageInfo, [FromQuery] IdleModel IdleModel)
        {
            if (string.IsNullOrEmpty(Convert.ToString(IdleModel.institutionId)))
            {
                dynamic response = await _coordinateChangeFeedbackBackgroundService.GetAllVehicleDetail(pageInfo, IdleModel);
                return StatusCode((int)response.statusCode, response);
            }
            else
            {
                dynamic response = await _coordinateChangeFeedbackBackgroundService.GetAllVehicleByInstitutionId(IdleModel);
                return StatusCode((int)response.statusCode, response);
            }
        }
    }
}
