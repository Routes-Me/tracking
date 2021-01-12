using System;
using System.Collections.Generic;
using System.Text;

namespace TrackService.RethinkDb_Changefeed
{
    public class Common
    {
        public static dynamic ThrowException(string message, int statusCode)
        {
            var ex = new Exception();
            ex.Data.Add(message, statusCode);
            throw ex;
        }
    }
}
