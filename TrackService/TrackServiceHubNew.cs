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
                //string deviceId = Context.GetHttpContext().Request.Query["deviceId"].ToString();

                Context.Items.Add("InstitutionId", institutionId);
                Context.Items.Add("VehicleId", vehicleId);

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
            var vehicleId = Context.Items["VehicleId"].ToString();


            string DeviceId = "test";
            var updates = "{\"vehicleId\": \"" + vehicleId + "\",\"institutionId\": \"" + instituitonId + "\",\"deviceId\": \"" + DeviceId;

            foreach (Location location in locations)
            {
                updates = updates + "\",\"coordinates\": {\"latitude\": \"" + location.Latitude + "\", \"longitude\": \"" + location.Longitude + "\",\"timestamp\": \"" + location.Timestamp + "\"}}";

            }

        }

        public async void SendLocationFeeds(List<Location> locations)
        {
            //await PublishFeeds(locations, Context);
        }

        //Receive feeds
        public async void SendLocation(string locations)
        {

            Feeds feeds = Newtonsoft.Json.JsonConvert.DeserializeObject<Feeds>(locations);

            string instituitonId = Context.Items["InstitutionId"].ToString();
            string vehicleId = Context.Items["VehicleId"].ToString();
            Console.WriteLine("Hub Log : START : vehicle ID : " + vehicleId);

            string deviceId = "---";

            var lastUpdate = " !!!Empty location feeds!!!";
            foreach (Location location in feeds.SendLocation)
            {
                lastUpdate = "Last location feed ::  Time -- " + location.Timestamp+" -- Location : Lat " + location.Latitude + " || " + "Long " + location.Longitude;
                var feed = "{\"vehicleId\": \"" + vehicleId + "\",\"institutionId\": \"" + instituitonId + "\",\"deviceId\": \"" + deviceId + "\",\"coordinates\": {\"latitude\": \"" + location.Latitude + "\", \"longitude\": \"" + location.Longitude + "\",\"timestamp\": \"" + location.Timestamp + "\"}}";
                await Clients.All.SendAsync("FeedsReceiver", feed);

            }

            //await PublishFeeds(feeds.SendLocation, Context);
            Console.WriteLine("Hub Log : END   : vehicle ID : " + vehicleId + " :: " + lastUpdate);
            await Clients.Client(Context.ConnectionId).SendAsync("CommonMessage", "{ \"code\":\"200\", \"message\": Coordinates inserted */successfully\"\" }");
            //await Clients.Groups(instituitonId,"super").SendAsync("FeedsReceiver", updates);
            
            
        }

        //Receiver Subscribe
        public async void Subscribe(string institutionId, string vehicleId, string deviceId)
        {
            
            try
            {
                //var claimData = GetUserClaimsData();
                //if (claimData.Privilege.Equals("super"))
                //{
                //    await Groups.AddToGroupAsync(Context.ConnectionId, "super");
                //}
                //else
                //{
                //    await Groups.AddToGroupAsync(Context.ConnectionId, institutionId);
                //}
            }
            catch(Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("CommonMessage", ex.Message);
                return;
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
