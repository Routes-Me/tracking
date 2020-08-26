using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RethinkDb.Driver.Net;
using TrackService.Models;
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








