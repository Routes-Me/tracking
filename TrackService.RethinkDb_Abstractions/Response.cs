using System;
using System.Collections.Generic;
using System.Text;
using VehicleData = TrackService.RethinkDb_Abstractions.VehicleDetails;

namespace TrackService.RethinkDb_Abstractions
{
    public class Response
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public ResponseCode responseCode { get; set; }

    }
    public enum ResponseCode
    {
        Success = 200,
        Error = 2,
        InternalServerError = 500,
        MovedPermanently = 301,
        NotFound = 404,
        BadRequest = 400,
        Conflict = 409,
        Created = 201,
        NotAcceptable = 406,
        Unauthorized = 401,
        RequestTimeout = 408,
        BadGateway = 502,
        ServiceUnavailable = 503,
        GatewayTimeout = 504,
        Permissionserror = 403,
        Forbidden = 403,
        TokenRequired = 499,
        InvalidToken = 498
    }

    public class VehicleResponse : Response
    {
        public List<VehicleData> Data { get; set; }
    }
}
