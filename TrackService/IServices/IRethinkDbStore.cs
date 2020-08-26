using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrackService.IServices
{
   public interface IRethinkDbStore
    {
        void InitializeDatabase();
    }
}
