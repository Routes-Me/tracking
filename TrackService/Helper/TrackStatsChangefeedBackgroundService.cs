﻿using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Obfuscation;
using System;
using System.Threading;
using System.Threading.Tasks;
using TrackService.RethinkDb_Abstractions;

namespace TrackService.Helper
{
    internal class CoordinateChangeFeedbackBackgroundService : BackgroundService
    {
        private readonly ICoordinateChangeFeedbackBackgroundService _coordinateChangeFeedbackBackgroundService;
        private readonly IHubContext<TrackServiceHub> _hubContext;
        TrackServiceHub trackServiceHub;

        public CoordinateChangeFeedbackBackgroundService(ICoordinateChangeFeedbackBackgroundService coordinateChangeFeedbackBackgroundService, IHubContext<TrackServiceHub> hubContext)
        {
            _coordinateChangeFeedbackBackgroundService = coordinateChangeFeedbackBackgroundService;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _coordinateChangeFeedbackBackgroundService.EnsureDatabaseCreated();

                IChangefeed<Coordinates> coordinateChangeFeedback = await _coordinateChangeFeedbackBackgroundService.GetCoordinatesChangeFeedback(stoppingToken);

                await foreach (Coordinates coordinateChange in coordinateChangeFeedback.FetchFeed(stoppingToken))
                {
                    if (coordinateChange != null)
                    {
                        string InstitutionId = string.Empty, DeviceId = string.Empty, VehicleId = string.Empty;
                        string newThreadStats = coordinateChange.ToString();
                        var mobileId = newThreadStats.Split(",")[0].Replace("mobileId:", "").Trim();
                        await Task.Run(() => { InstitutionId = _coordinateChangeFeedbackBackgroundService.GetInstitutionId(mobileId); }).ConfigureAwait(false);
                        await Task.Run(() => { VehicleId = _coordinateChangeFeedbackBackgroundService.GetVehicleId(mobileId); }).ConfigureAwait(false);
                        DeviceId = newThreadStats.Split(",")[4].Replace("deviceId:", "").Trim();
                        var Latitude = newThreadStats.Split(",")[1].Replace("latitude:", "").Trim();
                        var Longitude = newThreadStats.Split(",")[2].Replace("longitude:", "").Trim();
                        var timestamp = newThreadStats.Split(",")[3].Replace("timestamp:", "").Trim();
                        var institutionIdEncrypted = _coordinateChangeFeedbackBackgroundService.IdEncryption(Convert.ToInt32(InstitutionId));
                        var vehicleIdEncrypted = _coordinateChangeFeedbackBackgroundService.IdEncryption(Convert.ToInt32(VehicleId));
                        var deviceIdEncrypted = _coordinateChangeFeedbackBackgroundService.IdEncryption(Convert.ToInt32(DeviceId));
                        var json = "{\"vehicleId\": \"" + vehicleIdEncrypted + "\",\"institutionId\": \"" + institutionIdEncrypted + "\",\"deviceId\": \"" + deviceIdEncrypted + "\",\"coordinates\": {\"latitude\": \"" + Latitude + "\", \"longitude\": \"" + Longitude + "\",\"timestamp\": \"" + timestamp + "\"}}";
                        trackServiceHub = new TrackServiceHub();
                        await Task.Run(() => { trackServiceHub.SendDataToDashboard(_hubContext, institutionIdEncrypted, vehicleIdEncrypted, json); }).ConfigureAwait(true); // To send data to all subscribe vehicled for admin
                    }
                }
            }
            catch (OperationCanceledException) { }
        }
    }
}
