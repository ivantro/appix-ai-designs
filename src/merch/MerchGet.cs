using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;

namespace com.appix.ai.design {
    public class MerchGet{
        private readonly ILogger<MerchGet> _logger;

        public MerchGet(ILogger<MerchGet> logger){
            _logger = logger;
        }

        [Function(nameof(MerchGetById))]
        public IActionResult MerchGetById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "merch/{id}")] HttpRequest req,
            [CosmosDBInput(
                databaseName: "ai-designs",
                containerName: "merch",
                Connection = "CosmosDbConnectionString",
                Id = "{id}",
                PartitionKey = "appix")] MerchPOCO aMerch,
                string id) {
    
                _logger.LogInformation($"ITR..MerchGetById(): Retrieving item with id: {id}");

                if (aMerch == null) {
                    return new NotFoundResult();
                }
                return new OkObjectResult(aMerch);
        }

        [Function(nameof(MerchGetAll))]
        public async Task<HttpResponseData> MerchGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "merch")] HttpRequestData req,
            [CosmosDBInput(Connection = "CosmosDbConnectionString")] CosmosClient client) {
            _logger.LogInformation("ITR..MerchGetAll(): Function processed a request.");

            var container = client.GetContainer(Constants.DATABASE_NAME, Constants.TABLE_MERCH);
            var queryDefinition = new QueryDefinition("SELECT * FROM c");

            var iterator = container.GetItemQueryIterator<MerchPOCO>(queryDefinition);

            List<MerchPOCO> resultList = new List<MerchPOCO>();

            while (iterator.HasMoreResults) {
                var documents = await iterator.ReadNextAsync();
                resultList.AddRange(documents); // Add results to the list
            }

            if (resultList.Count > 0) {
                _logger.LogInformation($"Returning {resultList.Count} merch.");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(resultList); // Serialize and return the list
                return response;
            }

            return req.CreateResponse(HttpStatusCode.NoContent); // Return 204 if no data is found
        }  
    }
}
