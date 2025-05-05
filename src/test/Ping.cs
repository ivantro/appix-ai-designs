using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace com.appix.ai.design
{
    public class Ping
    {
        private readonly ILogger<Ping> _logger;

        public Ping(ILogger<Ping> logger)
        {
            _logger = logger;
        }

        [Function("Ping")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions2!");
        }
    }
}
