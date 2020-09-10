using RethinkDb.Driver.Net;
using TrackService.RethinkDb_Changefeed.Model;

namespace TrackService.IServices
{
    public interface IRethinkDbConnectionFactory
    {
        Connection CreateConnection();
        void CloseConnection();
        RethinkDbOptions GetOptions();
    }
}








