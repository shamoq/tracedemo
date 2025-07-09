using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TraceDemo.Core;

namespace TraceDemo.Controllers
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
            var MyActivitySource = new ActivitySource("demo");
            using var activity = MyActivitySource.StartActivity("Get2Async");
            activity?.SetTag("param1", 1);
            activity?.SetTag("param2", 2);

            await myService.CalculateAsync(1, 2);
            return "test";
        }

        [HttpGet]
        public async Task<string> Http() {

            // 用http调用baidu 首页
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://www.baidu.com");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            else
            {
                return "Error: " + response.StatusCode;
            }
        }
    }
}
