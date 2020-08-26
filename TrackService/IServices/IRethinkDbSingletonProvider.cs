using RethinkDb.Driver;
using RethinkDb.Driver.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrackService.IServices
{
    internal interface IRethinkDbSingletonProvider
    {
        RethinkDB RethinkDbSingleton { get; }
        Connection RethinkDbConnection { get; }
    }
}
