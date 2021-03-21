using TrackService.RethinkDb_Abstractions;

namespace TrackService.Abstraction
{
    public interface ILocationFeedsRepository
    {
        void InsertLocationFeeds(CordinatesModel cordinatesModel);
    }
}
