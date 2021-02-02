using TrackService.RethinkDb_Abstractions;

namespace TrackService.Abstraction
{
    public interface ILocationFeedsRepository
    {
        dynamic InsertLocationFeeds(CordinatesModel cordinatesModel);
    }
}
