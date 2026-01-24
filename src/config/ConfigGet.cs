using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace com.appix.ai.design {
    public class ConfigGet {
        private readonly ILogger<ConfigGet> _logger;

        public ConfigGet(ILogger<ConfigGet> logger) {
            _logger = logger;
        }

        [Function(nameof(GetConfig))]
        public IActionResult GetConfig(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "config/{applicationName}")] HttpRequest req,
            [CosmosDBInput(
                databaseName: "ai-designs",
                containerName: "config",
                Connection = "CosmosDbConnectionString",
                Id = "{applicationName}",
                PartitionKey = "appix")] ConfigPOCO config,
            string applicationName) {
    
            _logger.LogInformation($"ITR..GetConfig(): Retrieving config for application: {applicationName}");

            if (config == null) {
                return new NotFoundResult();
            }
            return new OkObjectResult(config);
        }
    }
}

