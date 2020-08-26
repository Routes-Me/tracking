using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrackService.Models;
using TrackService.RethinkDb_Abstractions;

namespace TrackService.Helper
{
    public class ThreadStatsChangefeedDbService //: IThreadStatsChangefeedDbService
    {
        private readonly IThreadStatsChangefeedDbService _threadStatsChangefeedDbService;
        public ThreadStatsChangefeedDbService(IThreadStatsChangefeedDbService threadStatsChangefeedDbService)
        {
            _threadStatsChangefeedDbService = threadStatsChangefeedDbService;
        }

        public Task EnsureDatabaseCreatedAsync()
        {
            throw new NotImplementedException();
        }
        
        public Task<IChangefeed<ThreadStats>> GetThreadStatsChangefeedAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task InsertThreadStatsAsync(ThreadStats threadStats)
        {
            await _threadStatsChangefeedDbService.InsertThreadStatsAsync(new ThreadStats
            {
                VehicleId = threadStats.VehicleId,
                Longitude = threadStats.Longitude,
                Latitude = threadStats.Latitude,
                TimeStamp = threadStats.TimeStamp


            });
        }

    }
}
