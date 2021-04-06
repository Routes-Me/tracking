using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using TrackService.Abstraction;
using TrackService.RethinkDb_Abstractions;
using TrackService.RethinkDb_Changefeed.DataAccess.Abstraction;
using Microsoft.Extensions.DependencyInjection;

namespace TrackService.Repository
{
    public class LocationFeedsRepository : ILocationFeedsRepository
    {
        private IDataAccessRepository _dataAccessRepo;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public IBackgroundTaskQueue Queue { get; }
        public LocationFeedsRepository(IBackgroundTaskQueue queue, ILogger<LocationFeedsRepository> logger, IServiceScopeFactory serviceScopeFactory, IDataAccessRepository dataAccessRepo)
        {
            _logger = logger;
            Queue = queue;
            _serviceScopeFactory = serviceScopeFactory;
            _dataAccessRepo = dataAccessRepo;
        }
        public void InsertLocationFeeds(VehicleData vehicleData)
        {
            Queue.QueueBackgroundWorkItem(async token =>
            {
                var guid = Guid.NewGuid().ToString();

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    await _dataAccessRepo.InsertCordinates(vehicleData);
                }

                _logger.LogInformation(
                    "Queued Background Task {Guid} is complete.", guid);
            });
        }
    }
}