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

    [Authorize] // (AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)
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

                institutionId = string.IsNullOrEmpty(institutionId) ? institutionId : Obfuscation.Decode(institutionId).ToString();
                vehicleId = string.IsNullOrEmpty(vehicleId) ? vehicleId : Obfuscation.Decode(vehicleId).ToString();
                deviceId = string.IsNullOrEmpty(deviceId) ? deviceId : Obfuscation.Decode(deviceId).ToString();

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

        private async Task PublishFeeds(IEnumerable<Location> locations)
        {
            string instituitonId = Context.Items["InstitutionId"].ToString();
            string vehicleId = Context.Items["VehicleId"].ToString();
            string deviceId = Context.Items["DeviceId"].ToString();

            Location lastLocationFeed = locations.Last();

            Console.WriteLine("Hub Log : " + "vehicle ID  - " + vehicleId + " - Institution ID  - " + instituitonId + " - Device ID  - " + deviceId + " -");
            Console.WriteLine(locations.Count() + " > location feed ::  Time -> " + lastLocationFeed.Timestamp + " <- Location : Lat " + lastLocationFeed.Latitude + " || " + "Long " + lastLocationFeed.Longitude);
            var feed = FeedFormat(locations.Last(), vehicleId: vehicleId, instituitonId: instituitonId, deviceId: deviceId);
            
            
            await Clients.Groups(instituitonId, "super").SendAsync("FeedsReceiver", feed); // locations.Last()
            await Clients.Client(Context.ConnectionId).SendAsync("CommonMessage", "{ \"code\":\"200\", \"message\": Coordinates inserted */successfully\"\" }");
        }

        private string FeedFormat(Location location, string vehicleId, string instituitonId, string deviceId)
        {
            return  "{\"vehicleId\": \"" + vehicleId + "\",\"institutionId\": \"" + instituitonId + "\",\"deviceId\": \"" + deviceId + "\",\"coordinates\": {\"latitude\": \"" + location.Latitude + "\", \"longitude\": \"" + location.Longitude + "\",\"timestamp\": \"" + location.Timestamp + "\"}}";
        }


        public async Task PublishAndSave(List<Location> locations) 
        {
            await PublishFeeds(locations);
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
        public async void Subscribe(string institutionId, string vehicleId, string deviceId)
        {
            try
            {
                SubscribeFeeds(institutionId: institutionId);
            }catch(Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("CommonMessage", ex.Message);
            }
        }

        private async void SubscribeFeeds(string institutionId)
        {
            var claimData = GetUserClaimsData();
            if (IsSuperUserAccess(claimData.Privilege))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "super");
            }
            else
            {
                if (string.IsNullOrEmpty(institutionId))
                {
                    throw new ArgumentNullException();
                }
                await Groups.AddToGroupAsync(Context.ConnectionId, institutionId);
            }
        }

        private bool IsSuperUserAccess(string role)
        {
            if (role.Equals("super") || role.Equals("support"))
            {
                return true;
            }
            return false;
        }

        //Receiver Unsubscribe
        public async void Unsubscribe()
        {
            
            try
            {
                var claimData = GetUserClaimsData();
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
        private UserClaimsData GetUserClaimsData()
        {
            UserClaimsData userClaimsData = new UserClaimsData();
            var user = Context.User;
            foreach (var item in user.Claims)
            {
                 if (item.Type.ToLower() == "rol")
                {
                    // var value = System.Text.Encoding.GetEncoding("iso-8859-1").GetString(Convert.FromBase64String(item.Value));

                    // var rolesItem = item.Value.Replace("[", "").Replace("]", "").Replace("\"", "").Replace("{", "").Replace("}", "");
                    // var mainSplit = rolesItem.Split(',');
                    // var appSplit = mainSplit[0].Split(':');
                    // var prevSplit = mainSplit[1].Split(':');
                    userClaimsData.Application = "dashboard";
                    userClaimsData.Privilege = "super";
                }
                if (item.Type == "InstitutionId")
                {
                    userClaimsData.TokenInstitutionId = item.Value;
                }
            }
            return userClaimsData;
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
