using RethinkDb.Driver;
using RethinkDb.Driver.Net;

namespace TrackService.IServices
{
    internal interface IRethinkDbSingletonProvider
    {
        RethinkDB RethinkDbSingleton { get; }
        Connection RethinkDbConnection { get; }
    }
}
