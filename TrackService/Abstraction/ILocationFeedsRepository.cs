using System.Collections.Generic;
using TrackService.RethinkDb_Abstractions;
using System.Threading.Tasks;

namespace TrackService.Abstraction
{
    public interface ILocationFeedsRepository
    {
        void InsertLocationFeeds(VehicleData vehicleData);
    }
}
