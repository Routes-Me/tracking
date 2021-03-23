using System.Collections.Generic;
using TrackService.RethinkDb_Abstractions;

namespace TrackService.Abstraction
{
    public interface ILocationFeedsRepository
    {
        void InsertLocationFeeds(List<Location> locations, int institutionId, int vehicleId);
    }
}
