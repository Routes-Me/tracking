using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using TrackService.IServices;
using TrackService.Models;

namespace TrackService.Services
{
    public class TrackInformationService : ITrackInformationService
    {
        HubConnection _connection;
        private HubConnectionState _connectionState;
        private TrackingInformation _trackingInfo;

        public TrackInformationService(TrackingInformation trackingInfo)
        {
            if (_connection.State != HubConnectionState.Connected)
            {
                //we will change remoteIp and remotePort with the hosted url in configuraion
                _connection = new HubConnectionBuilder()
                    .WithUrl("https://" + "remoteIp" + ":" + "remotePort" + "/api/TrackServiceHub", options =>
                    {
                        options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets | Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents;
                    })
                    .Build();
                _connection.StartAsync();
                _trackingInfo = trackingInfo;
            }
        }

        // we will update the method to send model to repository to save in database
        public bool SetTrackingInformation(string trackingInfo)
        {
            //vehicle_id:100;lat:2000303;long:293923923424;date:12-09-2003T14:00:00

            if (trackingInfo != null)
            {
                if (_connectionState == HubConnectionState.Connected)
                {
                    List<string> dataList = trackingInfo.Split(';').ToList();
                    Dictionary<string, string> dict = new Dictionary<string, string>();

                    var result = new string[2];

                    for (int i = 0; dataList.Count >= i; i++)
                    {
                        if (i < 3)
                        {
                            result = dataList.ElementAt(i).Split(':');
                            dict.Add(result[0], result[1]);
                        }
                        else
                        {
                            var dateWord = dataList.ElementAt(3).Substring(0, 4);
                            var dateValue = dataList.ElementAt(3).Substring(5, dataList.ElementAt(3).Length - 5).Replace('T', ' ');
                            dict.Add(dateWord, dateValue);
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
    }
}
