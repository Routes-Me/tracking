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

namespace TrackService
{

    public class TrackServiceHubNew : Hub
    {
        private readonly ILocationFeedsRepository  _locationsFeedsRepo;
        private readonly ICoordinateChangeFeedbackBackgroundService _coordinateChangeFeedbackBackgroundService;
        public TrackServiceHubNew(ILocationFeedsRepository  locationsFeedsRepo,
                                  ICoordinateChangeFeedbackBackgroundService coordinateChangeFeedbackBackgroundService)
        {
            _locationsFeedsRepo = locationsFeedsRepo;
            _coordinateChangeFeedbackBackgroundService = coordinateChangeFeedbackBackgroundService;
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

        private async Task PublishFeeds(IEnumerable<Location> locations)
        {
            string instituitonId = Context.Items["InstitutionId"].ToString();
            string vehicleId = Context.Items["VehicleId"].ToString();

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

            string institutionId = Context.Items["InstitutionId"].ToString();
            string vehicleId = Context.Items["VehicleId"].ToString();
            string deviceId = Context.Items["DeviceId"].ToString();

            //Console.WriteLine("Hub Log : vehicle ID  - " + vehicleId+ " - : START : ");

            Console.WriteLine("Hub Log : "+"vehicle ID  - " + vehicleId + " - Institution ID  - " + institutionId + " - Device ID  - " + deviceId + " -");


            var lastUpdate = " !!!Empty location feeds!!!";

            if(feeds.SendLocation.Count()>0)
            {
                Location location = feeds.SendLocation.Last();
                lastUpdate = feeds.SendLocation.Count() + " > location feed ::  Time -> " + location.Timestamp + " <- Location : Lat " + location.Latitude + " || " + "Long " + location.Longitude;
                var feed = "{\"vehicleId\": \"" + vehicleId + "\",\"institutionId\": \"" + institutionId + "\",\"deviceId\": \"" + deviceId + "\",\"coordinates\": {\"latitude\": \"" + location.Latitude + "\", \"longitude\": \"" + location.Longitude + "\",\"timestamp\": \"" + location.Timestamp + "\"}}";

                await Clients.Others.SendAsync("FeedsReceiver", feed);
                _locationsFeedsRepo.InsertLocationFeeds(new CordinatesModel
                    {
                        mobileId = vehicleId,
                        longitude = location.Longitude,
                        latitude = location.Latitude,
                        timestamp = location.Timestamp.ToString(),
                        deviceId = Convert.ToInt32(deviceId),
                        institutionId = Convert.ToInt32(institutionId)
                    });
            }
            

            //await PublishFeeds(feeds.SendLocation, Context);
            Console.WriteLine("Hub Log : vehicle ID  - " + vehicleId + " - : LAST FEED : " + lastUpdate);
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
