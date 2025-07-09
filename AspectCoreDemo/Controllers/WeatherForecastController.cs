using AspectCoreDemo.Core;
using Microsoft.AspNetCore.Mvc;

namespace AspectCoreDemo.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IMyService myService;
        public WeatherForecastController(IMyService myService)
        {
            this.myService = myService;
        }

        [HttpGet]
        public string Get()
        {
            myService.DoSomething();
            return "test";
        }

        [HttpGet]
        public async Task<string> Get2Async()
        {
            await myService.CalculateAsync(1,2);
            return "test";
        }
    }
}
