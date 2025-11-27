using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;

namespace com.appix.ai.design {
    public class CategoriesGet{
        private readonly ILogger<CategoriesGet> _logger;

        public CategoriesGet(ILogger<CategoriesGet> logger){
            _logger = logger;
        }

        [Function(nameof(CategoriesGetById))]
        public IActionResult CategoriesGetById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "categories/{id}")] HttpRequest req,
            [CosmosDBInput(
                databaseName: "ai-designs",
                containerName: "categories",
                Connection = "CosmosDbConnectionString",
                Id = "{id}",
                PartitionKey = "appix")] CategoriesPOCO aCategories,
                string id) {
    
                _logger.LogInformation($"ITR..CategoriesGetById(): Retrieving item with id: {id}");

                if (aCategories == null) {
                    return new NotFoundResult();
                }
                return new OkObjectResult(aCategories);
        }

        [Function(nameof(CategoriesGetAll))]
        public async Task<HttpResponseData> CategoriesGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "categories")] HttpRequestData req,
            [CosmosDBInput(Connection = "CosmosDbConnectionString")] CosmosClient client) {
            _logger.LogInformation("ITR..CategoriesGetAll(): Function processed a request.");

            var container = client.GetContainer(Constants.DATABASE_NAME, Constants.TABLE_CATEGORIES);
            var queryDefinition = new QueryDefinition("SELECT * FROM c");

            var iterator = container.GetItemQueryIterator<CategoriesPOCO>(queryDefinition);

            List<CategoriesPOCO> resultList = new List<CategoriesPOCO>();

            while (iterator.HasMoreResults) {
                var documents = await iterator.ReadNextAsync();
                resultList.AddRange(documents); // Add results to the list
            }

            if (resultList.Count > 0) {
                _logger.LogInformation($"Returning {resultList.Count} categories.");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(resultList); // Serialize and return the list
                return response;
            }

            return req.CreateResponse(HttpStatusCode.NoContent); // Return 204 if no data is found
        }  
    }
}
