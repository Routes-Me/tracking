using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        [Route("vehicleStatus")]
        public async Task<IActionResult> VehicleStatus([FromQuery] IdleModel IdleModel)
        {
            VehicleResponse response = new VehicleResponse();
            try
            {
                if (ModelState.IsValid)
                {
                    List<VehicleDetails> vehicleDetails = new List<VehicleDetails>();

                    if (string.IsNullOrEmpty(Convert.ToString(IdleModel.institutionId)))
                        await Task.Run(() => { response = _coordinateChangeFeedbackBackgroundService.GetAllVehicleDetail(IdleModel); }).ConfigureAwait(false);
                    else
                        await Task.Run(() => { response = _coordinateChangeFeedbackBackgroundService.GetAllVehicleByInstitutionId(IdleModel); }).ConfigureAwait(false);

                    if (response.responseCode != ResponseCode.Success)
                        return NotFound(response);
                    else
                        return Ok(response);
                }
                else
                {
                    response.Status = false;
                    response.Message = "Validation error.";
                    response.responseCode = ResponseCode.Forbidden;
                    response.Data = null;
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Message = "Something went wrong while getting vehicle data. Error - " + ex.Message;
                response.responseCode = ResponseCode.InternalServerError;
                response.Data = null;
                return BadRequest(response);
            }
        }
    }
}
