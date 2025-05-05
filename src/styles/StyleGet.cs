using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;

namespace com.appix.ai.design {
    public class StyleGet{
        private readonly ILogger<StyleGet> _logger;

        public StyleGet(ILogger<StyleGet> logger){
            _logger = logger;
        }

        [Function(nameof(StyleGetById))]
        public IActionResult StyleGetById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "styles/{id}")] HttpRequest req,
            [CosmosDBInput(
                databaseName: "ai-designs",
                containerName: "styles",
                Connection = "CosmosDbConnectionString",
                Id = "{id}",
                PartitionKey = "appix")] StylePOCO aStyle,
                string id) {
    
                _logger.LogInformation($"ITR..StyleGetById(): Retrieving item with id: {id}");

                if (aStyle == null) {
                    return new NotFoundResult();
                }
                return new OkObjectResult(aStyle);
        }

        [Function(nameof(StyleGetAll))]
        public async Task<HttpResponseData> StyleGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "styles")] HttpRequestData req,
            [CosmosDBInput(Connection = "CosmosDbConnectionString")] CosmosClient client) {
            _logger.LogInformation("ITR..StyleGetAll(): Function processed a request.");

            var container = client.GetContainer(Constants.DATABASE_NAME, Constants.TABLE_STYLES);
            var queryDefinition = new QueryDefinition("SELECT * FROM c");

            var iterator = container.GetItemQueryIterator<StylePOCO>(queryDefinition);

            List<StylePOCO> resultList = new List<StylePOCO>();

            while (iterator.HasMoreResults) {
                var documents = await iterator.ReadNextAsync();
                resultList.AddRange(documents); // Add results to the list
            }

            if (resultList.Count > 0) {
                _logger.LogInformation($"Returning {resultList.Count} styles.");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(resultList); // Serialize and return the list
                return response;
            }

            return req.CreateResponse(HttpStatusCode.NoContent); // Return 204 if no data is found
        }  
    }
}
