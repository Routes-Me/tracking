using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;

namespace TrackService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        [Obsolete]
        public readonly IHostingEnvironment _hostingEnv;

        [Obsolete]
        public HomeController(IHostingEnvironment hostingEnv)
        {
            _hostingEnv = hostingEnv;
        }
        [HttpGet]
        [Obsolete]
        public string Get()
        {
            return "Tracking service started successfully. Environment - " + _hostingEnv.EnvironmentName + "";
        }
    }
}
