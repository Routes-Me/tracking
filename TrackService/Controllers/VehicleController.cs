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
        public IActionResult VehicleStatus(string vehicleId, [FromQuery] Pagination pageInfo, [FromQuery] IdleModel idleModel)
        {
            try
            {
                VehicleResponse response = new VehicleResponse();
                var vehicles = _coordinateChangeFeedbackBackgroundService.GetVehicles(vehicleId, pageInfo, idleModel);
                var page = new Pagination
                {
                    offset = pageInfo.offset,
                    limit = pageInfo.limit,
                    total = vehicles.Item2
                };
                response.status = true;
                response.message = "Vehicle retrived successfully.";
                response.statusCode = StatusCodes.Status200OK;
                response.data = vehicles.Item1;
                response.pagination = page;
                return StatusCode((int)response.statusCode, response);
            }
            catch (Exception ex)
            {
                dynamic errorResponse = ReturnResponse.ExceptionResponse(ex);
                return StatusCode((int)errorResponse.statusCode, errorResponse);
            }
        }
        [HttpDelete]
        [Route("tracking/vehicles/{vehicleId?}")]
        public IActionResult ClearLiveDatabase(string vehicleId)
        {
            try
            {
                _coordinateChangeFeedbackBackgroundService.ClearLiveTrackingDatabase(vehicleId);
                dynamic response = ReturnResponse.SuccessResponse("Records removed from live tracking service successfully.", false);
                return StatusCode((int)response.statusCode, response);
            }
            catch (NullReferenceException ex)
            {
                return StatusCode(StatusCodes.Status404NotFound, ex.Message);
            }
            catch (Exception ex)
            {
                dynamic errorResponse = ReturnResponse.ExceptionResponse(ex);
                return StatusCode((int)errorResponse.statusCode, errorResponse);
            }
        }
    }
}
