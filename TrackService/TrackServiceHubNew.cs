using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using TrackService.Models;

namespace TrackService
{
    
    public class TrackServiceHubNew : Hub
    {

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

        public async void SendLocation(List<Location> locations)
        {
            await PublishFeeds(locations);
        }

        public async void SendLocation(string locations)
        {
            Feeds feeds = Newtonsoft.Json.JsonConvert.DeserializeObject<Feeds>(locations);

            await PublishFeeds(feeds.SendLocation);
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
            if (claimData.Privilege.Equals("super"))
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
                if (item.Type.ToLower() == "roles")
                {
                    var rolesItem = item.Value.Replace("[", "").Replace("]", "").Replace("\"", "").Replace("{", "").Replace("}", "");
                    var mainSplit = rolesItem.Split(',');
                    var appSplit = mainSplit[0].Split(':');
                    var prevSplit = mainSplit[1].Split(':');
                    userClaimsData.Application = appSplit[1];
                    userClaimsData.Privilege = prevSplit[1];
                }
                if (item.Type == "InstitutionId")
                {
                    userClaimsData.TokenInstitutionId = item.Value;
                }
            }
            return userClaimsData;
        }

    }
}
