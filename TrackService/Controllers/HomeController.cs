using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;

namespace TrackService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        public readonly IWebHostEnvironment _hostingEnv;
        public HomeController(IWebHostEnvironment hostingEnv)
        {
            _hostingEnv = hostingEnv;
        }
        [HttpGet]
        public string Get()
        {
            return "Tracking service started successfully. Environment - " + _hostingEnv.EnvironmentName + "";
        }
    }
}
