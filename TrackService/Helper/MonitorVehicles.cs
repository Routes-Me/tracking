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

        public MonitorVehicles(ICoordinateChangeFeedbackBackgroundService threadStatsChangefeedDbService)
        {
            _coordinateChangeFeedbackBackgroundService = threadStatsChangefeedDbService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var autoEvent = new AutoResetEvent(false);
            var autoEventFrequent = new AutoResetEvent(false);
            var statusChecker = new StatusChecker(_coordinateChangeFeedbackBackgroundService);
            await Task.Run(() => { new Timer(statusChecker.CheckStatus, autoEvent, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60)); autoEvent.WaitOne(); }).ConfigureAwait(false);
        }
    }

    internal class StatusChecker
    {
        private readonly ICoordinateChangeFeedbackBackgroundService _oordinateChangeFeedbackBackgroundService;
        public StatusChecker(ICoordinateChangeFeedbackBackgroundService oordinateChangeFeedbackBackgroundService)
        {
            _oordinateChangeFeedbackBackgroundService = oordinateChangeFeedbackBackgroundService;
        }

        public void CheckStatus(Object stateInfo)
        {
            _oordinateChangeFeedbackBackgroundService.UpdateVehicleStatus();
        }
    }
}
