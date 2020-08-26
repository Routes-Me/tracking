using RethinkDb.Driver;
using RethinkDb.Driver.Net;

namespace TrackService.RethinkDb_Changefeed
{
    public interface IRethinkDbSingletonProvider
    {
        RethinkDB RethinkDbSingleton { get; }

        Connection RethinkDbConnection { get; }
    }
}
