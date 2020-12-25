﻿using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using TrackService.RethinkDb_Abstractions;

namespace TrackService.Helper
{
    internal class MonitorVehicles : BackgroundService
    {
        private readonly ICoordinateChangeFeedbackBackgroundService _coordinateChangeFeedbackBackgroundService;
        private readonly IHubContext<TrackServiceHub> _hubContext;
        TrackServiceHub trackServiceHub;

        public MonitorVehicles(ICoordinateChangeFeedbackBackgroundService threadStatsChangefeedDbService, IHubContext<TrackServiceHub> hubContext)
        {
            _coordinateChangeFeedbackBackgroundService = threadStatsChangefeedDbService;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var autoEvent = new AutoResetEvent(false);
            await Task.Run(() => { new Timer(UpdateVehicleAsync, autoEvent, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60)); autoEvent.WaitOne(); }).ConfigureAwait(false);
        }

        private async void UpdateVehicleAsync(object state)
        {
            var idealVehicles = _coordinateChangeFeedbackBackgroundService.UpdateVehicleStatus();
            foreach (var item in idealVehicles)
            {
                string vehicleId = _coordinateChangeFeedbackBackgroundService.IdEncryption(Convert.ToInt32(item));
                var json = "The vehicleId " + vehicleId + " has gone into the offline state.";
                trackServiceHub = new TrackServiceHub();
                await Task.Run(() => { trackServiceHub.NotifyIdealVehicleStatusToDashborad(_hubContext, json); }).ConfigureAwait(true); // Send notification to dashboard when any vehicle goes into offline state.
            }
        }
    }
}
