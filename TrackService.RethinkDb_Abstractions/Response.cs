using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using VehicleData = TrackService.RethinkDb_Abstractions.VehicleDetails;

namespace TrackService.RethinkDb_Abstractions
{
    public class Response
    {
        public bool status { get; set; }
        public string message { get; set; }
        public int statusCode { get; set; }
    }

    public class ReturnResponse
    {
        public static dynamic ExceptionResponse(Exception ex)
        {
            Response response = new Response();
            response.status = false;
            response.message = "Something went wrong. Error Message - " + ex.Message;
            response.statusCode = StatusCodes.Status500InternalServerError;
            return response;
        }

        public static dynamic SuccessResponse(string message, bool isCreated)
        {
            Response response = new Response();
            response.status = true;
            response.message = message;
            if (isCreated)
                response.statusCode = StatusCodes.Status201Created;
            else
                response.statusCode = StatusCodes.Status200OK;
            return response;
        }

        public static dynamic ErrorResponse(string message, int statusCode)
        {
            Response response = new Response();
            response.status = false;
            response.message = message;
            response.statusCode = statusCode;
            return response;
        }
    }

    public class Pagination
    {
        public int offset { get; set; } = 1;
        public int limit { get; set; } = 10000;
        public int total { get; set; }
    }


    public class VehicleResponse : Response
    {
        public Pagination pagination { get; set; }
        public List<VehicleData> data { get; set; }
    }
}
