using System;
using System.Threading;
using System.Threading.Tasks;
using TrackService.Helper.CronJobServices.CronJobExtensionMethods;
using TrackService.RethinkDb_Abstractions;

namespace TrackService.Helper.CronJobServices
{
    public class SyncCoordinates : CronJobService
    {
        private readonly ICoordinateChangeFeedbackBackgroundService _threadStatsChangefeedDbService;

        public SyncCoordinates(IScheduleConfig<SyncCoordinates> config, ICoordinateChangeFeedbackBackgroundService threadStatsChangefeedDbService) : base(config.CronExpression, config.TimeZoneInfo)
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
                _threadStatsChangefeedDbService.SyncCoordinatesToArchiveTable();
            }
            catch (Exception ex)
            {
                WriteLog.WriteLogToFile( "Error in SyncCoordinate. Error: " + " " + ex.Message);
            }
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }
    }
}
