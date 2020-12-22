using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using TrackService.Helper;
using TrackService.RethinkDb_Abstractions;
using TrackService.Helper.ConnectionMapping;
using TrackService.Models;
using Microsoft.AspNetCore.Authorization;
using Obfuscation;
using TrackService.RethinkDb_Changefeed.Model.Common;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Newtonsoft.Json;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace TrackService
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TrackServiceHub : Hub
    {
        public readonly static ConnectionMapping<string> _connections = new ConnectionMapping<string>();
        public readonly static AllVehicleMapping<string> _all = new AllVehicleMapping<string>();
        public readonly static InstitutionsMapping<string> _institutions = new InstitutionsMapping<string>();
        public readonly static VehiclesMapping<string> _vehicles = new VehiclesMapping<string>();

        public readonly static InstitutionId<string> _institutionsId = new InstitutionId<string>();
        public readonly static VehicleID<string> _vehiclesId = new VehicleID<string>();
        public readonly static DevicesId<string> _deviceId = new DevicesId<string>();

        private readonly ICoordinateChangeFeedbackBackgroundService _coordinateChangeFeedbackBackgroundService;

        public TrackServiceHub()
        {
        }

        public TrackServiceHub(ICoordinateChangeFeedbackBackgroundService coordinateChangeFeedbackBackgroundService)
        {
            _coordinateChangeFeedbackBackgroundService = coordinateChangeFeedbackBackgroundService;
        }

        public async void SendLocation(string locations)
        {
            try
            {
                LocationFeed feeds = Newtonsoft.Json.JsonConvert.DeserializeObject<LocationFeed>(locations);
                foreach (var location in feeds.SendLocation)
                {
                    await _coordinateChangeFeedbackBackgroundService.InsertCordinates(new CordinatesModel
                    {
                        mobileId = _vehiclesId.GetVehicleId(Context.ConnectionId).ToString(),
                        longitude = location.longitude,
                        latitude = location.latitude,
                        timestamp = location.timestamp.ToString(),
                        deviceId = Convert.ToInt32(_deviceId.GetDeviceId(Context.ConnectionId))
                    });
                }
                await Clients.Client(Context.ConnectionId).SendAsync("CommonMessage", "{ \"code\":\"200\", \"message\": Coordinates inserted successfully\"\" }");
            }
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("CommonMessage", "{ \"code\":\"103\", \"message\":\"Error:\"" + ex.Message + " }");
            }
        }

        public async void Subscribe(string InstitutionId, string VehicleId, string All)
        {
            try
            {
                if (!string.IsNullOrEmpty(InstitutionId))
                {
                    SubscribeInstitution(InstitutionId);
                }
                else if (!string.IsNullOrEmpty(VehicleId))
                {
                    SubscribeVehicle(InstitutionId, VehicleId);
                }
                else if (!string.IsNullOrEmpty(All) && All.Equals("--all"))
                {
                    SubscribeAll(All);
                }
                else
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("CommonMessage", "{ \"code\":\"107\", \"message\":\"Please provide atleast one parameter!\" }");
                    return;
                }
            }
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("CommonMessage", ex.Message);
                return;
            }
        }

        public void Unsubscribe()
        {
            _all.RemoveAll(Context.ConnectionId);
            _institutions.RemoveAll(Context.ConnectionId);
            _vehicles.RemoveAll(Context.ConnectionId);
        }

        public async void SendDataToDashboard(IHubContext<TrackServiceHub> context, ICoordinateChangeFeedbackBackgroundService _coordinateChangeFeedbackBackgroundService, string institutionId, string vehicleId, string json)
        {
            int institutionIdDecrypted = _coordinateChangeFeedbackBackgroundService.IdDecryption(institutionId);
            int vehicleIdDecrypted = _coordinateChangeFeedbackBackgroundService.IdDecryption(vehicleId);

            // Send data to ReceiveAll screen
            await context.Clients.All.SendAsync("ReceiveAllData", json);

            // Send all vehicle data to Admin.
            foreach (var connectionid in _all.GetAll_ConnectionId("--all"))
            {
                await context.Clients.Client(connectionid).SendAsync("ReceiveAllVehicleData", json);
            }

            // Send all vehicle for institution.
            foreach (var connectionid in _institutions.GetInstitution_ConnectionId(institutionIdDecrypted.ToString()))
            {
                await context.Clients.Client(connectionid).SendAsync("ReceiveInstitutionData", json);
            }

            // Send vehicle data for vehicle.
            foreach (var connectionid in _vehicles.GetVehicle_ConnectionId(vehicleIdDecrypted.ToString()))
            {
                await context.Clients.Client(connectionid).SendAsync("ReceiveVehicleData", json);
            }
        }

        public override async Task OnConnectedAsync()
        {
            if (Context.GetHttpContext().Request.Query.Keys.Count > 1)
            {
                var institutionId = Context.GetHttpContext().Request.Query["institutionId"].ToString();
                var vehicleId = Context.GetHttpContext().Request.Query["vehicleId"].ToString();
                var deviceId = Context.GetHttpContext().Request.Query["deviceId"].ToString();

                if (!string.IsNullOrEmpty(institutionId) && !string.IsNullOrEmpty(vehicleId) && !string.IsNullOrEmpty(deviceId))
                {
                    int institutionIdDecrypted = _coordinateChangeFeedbackBackgroundService.IdDecryption(institutionId);
                    int vehicleIdDecrypted = _coordinateChangeFeedbackBackgroundService.IdDecryption(vehicleId);
                    int deviceIdDecrypted = _coordinateChangeFeedbackBackgroundService.IdDecryption(deviceId);

                    _deviceId.Add(Context.ConnectionId, deviceIdDecrypted.ToString());
                    _vehiclesId.Add(Context.ConnectionId, vehicleIdDecrypted.ToString());
                    _institutionsId.Add(Context.ConnectionId, institutionIdDecrypted.ToString());

                    if (!string.IsNullOrEmpty(institutionId.ToString()) && !string.IsNullOrEmpty(vehicleId.ToString()))
                    {
                        await _coordinateChangeFeedbackBackgroundService.InsertMobiles(new MobilesModel
                        {
                            institutionId = institutionIdDecrypted,
                            vehicleId = vehicleIdDecrypted,
                            timestamp = DateTime.UtcNow.ToString()
                        });
                    }
                }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            _connections.Remove(Context.ConnectionId, Context.GetHttpContext().Request.Query["type"].ToString());
            if (!string.IsNullOrEmpty(Context.GetHttpContext().Request.Query["vehicleId"]))
            {
                var vehicleId = Context.GetHttpContext().Request.Query["vehicleId"].ToString();
                int vehicleIdDecrypted = _coordinateChangeFeedbackBackgroundService.IdDecryption(vehicleId);
                _coordinateChangeFeedbackBackgroundService.ChangeVehicleStatus(vehicleIdDecrypted.ToString());
            }
            _all.RemoveAll(Context.ConnectionId);
            _institutions.RemoveAll(Context.ConnectionId);
            _vehicles.RemoveAll(Context.ConnectionId);
            _deviceId.Remove(Context.ConnectionId);
            _vehiclesId.Remove(Context.ConnectionId);
            _institutionsId.Remove(Context.ConnectionId);
            await base.OnDisconnectedAsync(ex);
        }

        private void SubscribeInstitution(string InstitutionId)
        {
            var claimData = GetUserClaimsData();
            if (claimData.TokenInstitutionId == InstitutionId)
            {
                int institutionIdDecrypted = _coordinateChangeFeedbackBackgroundService.IdDecryption(InstitutionId);
                if (institutionIdDecrypted > 0)
                {
                    if (_coordinateChangeFeedbackBackgroundService.CheckInstitutionExists(institutionIdDecrypted.ToString()))
                    {
                        _institutions.Add(institutionIdDecrypted.ToString(), Context.ConnectionId);
                        return;
                    }
                    else
                    {
                        throw new Exception("{ \"code\":\"101\", \"message\":\"Institution does not exists!\" }");
                    }
                }
                else
                {
                    throw new Exception("{ \"code\":\"102\", \"message\":\"Bad request value. Invalid InstitutionId!\" }");
                }
            }
            else
            {
                bool isSuperInstitutions = _coordinateChangeFeedbackBackgroundService.SuperInstitutions(claimData.TokenInstitutionId);
                if (isSuperInstitutions)
                {
                    string All = "--all";
                    _all.Add(All, Context.ConnectionId);
                }
                else
                {
                    throw new Exception("{ \"code\":\"103\", \"message\":\"You are not allowed to subscribe to " + InstitutionId + "!\" }");
                }
            }
        }

        private void SubscribeVehicle(string InstitutionId, string VehicleId)
        {
            if (!string.IsNullOrEmpty(InstitutionId))
            {
                int vehicleIdDecrypted = _coordinateChangeFeedbackBackgroundService.IdDecryption(VehicleId);
                if (vehicleIdDecrypted > 0)
                {
                    if (_coordinateChangeFeedbackBackgroundService.CheckVehicleExists(vehicleIdDecrypted.ToString()))
                    {
                        _vehicles.Add(vehicleIdDecrypted.ToString(), Context.ConnectionId);
                        return;
                    }
                    else
                    {
                        throw new Exception("{ \"code\":\"104\", \"message\":\"Vehicle does not exists!\" }");
                    }
                }
                else
                {
                    throw new Exception("{ \"code\":\"105\", \"message\":\"Bad request value. Invalid VehicleId!\" }");
                }
            }
            else
            {
                throw new Exception("{ \"code\":\"108\", \"message\":\"Please subscribe institution first! }");
            }
        }

        private void SubscribeAll(string All)
        {
            var claimData = GetUserClaimsData();
            if (claimData.Application.ToLower() == "dashboard" && claimData.Privilege.ToLower() == "super")
            {
                _all.Add(All, Context.ConnectionId);
            }
            else
            {
                throw new Exception("{ \"code\":\"106\", \"message\":\"You are not allowed to subscribe! \" }");
            }
        }

        private UserClaimsData GetUserClaimsData()
        {
            UserClaimsData userClaimsData = new UserClaimsData();
            var user = Context.User;
            foreach (var item in user.Claims)
            {
                if (item.Type.ToLower() == "roles")
                {
                    var rolesItem = item.Value.Replace("[", "").Replace("]", "").Replace("\"", "").Replace("{", "").Replace("}", "");
                    var mainSplit = rolesItem.Split(',');
                    var appSplit = mainSplit[0].Split(':');
                    var prevSplit = mainSplit[1].Split(':');
                    userClaimsData.Application = appSplit[1];
                    userClaimsData.Privilege = prevSplit[1];
                }
                if (item.Type.ToLower() == "institutionid")
                {
                    userClaimsData.TokenInstitutionId = item.Value;
                }
            }
            return userClaimsData;
        }
    }
}