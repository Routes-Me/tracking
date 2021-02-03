using Microsoft.AspNetCore.SignalR;
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

        public MonitorVehicles(ICoordinateChangeFeedbackBackgroundService threadStatsChangefeedDbService, IHubContext<TrackServiceHub> hubContext)
        {
            _coordinateChangeFeedbackBackgroundService = threadStatsChangefeedDbService;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var autoEvent = new AutoResetEvent(false);
            // await Task.Run(() => { new Timer(UpdateVehicleAsync, autoEvent, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60)); autoEvent.WaitOne(); }).ConfigureAwait(false);
        }

    }
}
