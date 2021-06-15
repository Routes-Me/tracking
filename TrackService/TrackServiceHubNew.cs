using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using TrackService.Models;
using TrackService.Abstraction;
using TrackService.RethinkDb_Abstractions;
using TrackService.RethinkDb_Changefeed.Model.Common;
using Microsoft.Extensions.Options;
using RoutesSecurity;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace TrackService
{

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TrackServiceHubNew : Hub
    {
        // private readonly ILocationFeedsRepository  _locationsFeedsRepo;
        public TrackServiceHubNew() // ILocationFeedsRepository  locationsFeedsRepo,
        {
            // _locationsFeedsRepo = locationsFeedsRepo;
          
        }

        //Sender Connection established
        public override async Task OnConnectedAsync()
        {
            if(Context.GetHttpContext().Request.Query.Keys.Count > 1)
            {
                string institutionId = Context.GetHttpContext().Request.Query["institutionId"].ToString();
                string vehicleId = Context.GetHttpContext().Request.Query["vehicleId"].ToString();
                string deviceId = Context.GetHttpContext().Request.Query["deviceId"].ToString();

                Context.Items.Add("InstitutionId", institutionId);
                Context.Items.Add("VehicleId", vehicleId);
                Context.Items.Add("DeviceId", deviceId);
            }
            await base.OnConnectedAsync();
        }

        //Sender Disconnection
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            await base.OnDisconnectedAsync(ex);
        }

        private async Task PublishFeeds(Location location)
        {

            var rawInstitution = Context.Items["InstitutionId"].ToString();

            if(string.IsNullOrEmpty(rawInstitution) == true) {
                return;
            }

            string instituitonId = Obfuscation.Decode(rawInstitution).ToString();

            string vehicleId = Context.Items["VehicleId"].ToString();
            string deviceId = Context.Items["DeviceId"].ToString();

            await Clients.Groups(instituitonId, "super").SendAsync("FeedsReceiver", FeedFormat(location, vehicleId: vehicleId, instituitonId: rawInstitution, deviceId: deviceId));
        }

        private string FeedFormat(Location location, string vehicleId, string instituitonId, string deviceId)
        {
            return  "{\"vehicleId\": \"" + vehicleId + "\",\"institutionId\": \"" + instituitonId + "\",\"deviceId\": \"" + deviceId + "\",\"coordinates\": {\"latitude\": \"" + location.Latitude + "\", \"longitude\": \"" + location.Longitude + "\",\"timestamp\": \"" + location.Timestamp + "\"}}";
            // return new FeedsDto {
            //     InstitutionId = instituitonId,
            //     VehicleId = vehicleId,
            //     DeviceId = deviceId,
            //     Longitude = location.Longitude,
            //     Latitude = location.Latitude,
            //     Timestamp = location.Timestamp
            // };
        }


        public async Task PublishAndSave(List<Location> locations) 
        {
            await PublishFeeds(locations.First());
            // SaveFeeds(locations);
        }

        public async void SendLocations(List<Location> locations)
        {
           await PublishAndSave(locations: locations);
        }

        public async void SendLocation(string locations)
        {
            Feeds feeds = Newtonsoft.Json.JsonConvert.DeserializeObject<Feeds>(locations);
            await PublishAndSave(feeds.SendLocation);
        }


        //Receiver Subscribe
        public void Subscribe(string institutionId, string vehicleId, string deviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(institutionId))
                    SubscribeToAll();
                else
                   SubscribeToInstitution(institutionId);
            }
            catch (Exception ex)
            {
                Clients.Client(Context.ConnectionId).SendAsync("CommonMessage", ex.Message);
            }
        }

        private async void SubscribeToAll() {
            if (IsSuperPriviliged(GetUserClaims()))
                await SubscribeToGroup("super");
        }

        private async void SubscribeToInstitution(string institutionId) {
            if (string.IsNullOrEmpty(institutionId))
                return;

            await SubscribeToGroup(Obfuscation.Decode(institutionId).ToString()); //TODO: throw error if decode is null 
        }

        private async Task SubscribeToGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        private bool IsSuperPriviliged(UserClaimsData claims)
        {
            var isPriviliged = claims.Privilege.Equals("super") || claims.Privilege.Equals("support");
            var hasPortalAccess = claims.Application.Equals("dashboard");
            return isPriviliged && hasPortalAccess;
        }

        //Receiver Unsubscribe
        public async void Unsubscribe()
        {
            
            try
            {
                var claimData = GetUserClaims();
                if (claimData.Privilege.Equals("super"))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, "super");
                }
                else
                {
                    string instituitonId = Context.Items["InstitutionId"].ToString();
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, claimData.TokenInstitutionId);
                }
                    
            }
            catch(Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("CommonMessage", ex.Message);
                return;
            }
            
        }

        //Return User Cliams
        private UserClaimsData GetUserClaims()
        {
            return Context.User.Claims.Where(i => i.Type.ToLower() == "rol").Select(claim => {
                var value = System.Text.Encoding.GetEncoding("iso-8859-1").GetString(Convert.FromBase64String(claim.Value));

                var rolesItem = value.Replace("[", "").Replace("]", "").Replace("\"", "").Replace("{", "").Replace("}", "");
                var mainSplit = rolesItem.Split(',');
                var application = mainSplit[0].Split(':');
                var privilige = mainSplit[1].Split(':');

                return new UserClaimsData() {
                    Application = application.LastOrDefault(),
                    Privilege = privilige.LastOrDefault()
                };
            }).FirstOrDefault();
        }

        private void SaveFeeds(List<Location> locations)
        {
            VehicleData vehicleData = new VehicleData
            {
                InstitutionId = Convert.ToInt32(Context.Items["InstitutionId"]),
                VehicleId = Convert.ToInt32(Context.Items["VehicleId"]),
                DeviceId = Convert.ToInt32(Context.Items["DeviceId"]),
                Locations = locations
            };
            // _locationsFeedsRepo.InsertLocationFeeds(vehicleData);
        }
    }
}
