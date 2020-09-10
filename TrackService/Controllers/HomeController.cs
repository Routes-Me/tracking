using Microsoft.AspNetCore.Mvc;

namespace TrackService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            return "Server start successfully";
        }
    }
}
