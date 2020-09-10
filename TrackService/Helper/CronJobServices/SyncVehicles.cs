using System;
using System.Threading;
using System.Threading.Tasks;
using TrackService.Helper.CronJobServices.CronJobExtensionMethods;
using TrackService.RethinkDb_Abstractions;

namespace TrackService.Helper.CronJobServices
{
    public class SyncVehicles : CronJobService
    {
        private readonly ICoordinateChangeFeedbackBackgroundService _threadStatsChangefeedDbService;

        public SyncVehicles(IScheduleConfig<SyncVehicles> config, ICoordinateChangeFeedbackBackgroundService threadStatsChangefeedDbService) : base(config.CronExpression, config.TimeZoneInfo)
        {
            _threadStatsChangefeedDbService = threadStatsChangefeedDbService;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task DoWork(CancellationToken cancellationToken)
        {
            try
            {
                _threadStatsChangefeedDbService.SyncVehiclesToArchiveTable();
            }
            catch (Exception ex)
            {
                WriteLog.WriteLogToFile("Error in SyncVehicles. Error: " + " " + ex.Message);
            }
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }

    }
}
