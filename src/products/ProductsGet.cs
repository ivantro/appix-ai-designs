using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;

namespace com.appix.ai.design {
    public class ProductsGet{
        private readonly ILogger<ProductsGet> _logger;

        public ProductsGet(ILogger<ProductsGet> logger){
            _logger = logger;
        }

        [Function(nameof(ProductsGetById))]
        public IActionResult ProductsGetById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{id}")] HttpRequest req,
            [CosmosDBInput(
                databaseName: "ai-designs",
                containerName: "products",
                Connection = "CosmosDbConnectionString",
                Id = "{id}",
                PartitionKey = "appix")] ProductsPOCO aProducts,
                string id) {
    
                _logger.LogInformation($"ITR..ProductsGetById(): Retrieving item with id: {id}");

                if (aProducts == null) {
                    return new NotFoundResult();
                }
                return new OkObjectResult(aProducts);
        }

        [Function(nameof(ProductsGetAll))]
        public async Task<HttpResponseData> ProductsGetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req,
            [CosmosDBInput(Connection = "CosmosDbConnectionString")] CosmosClient client) {
            _logger.LogInformation("ITR..ProductsGetAll(): Function processed a request.");

            var container = client.GetContainer(Constants.DATABASE_NAME, Constants.TABLE_PRODUCTS);
            var queryDefinition = new QueryDefinition("SELECT * FROM c");

            var iterator = container.GetItemQueryIterator<ProductsPOCO>(queryDefinition);

            List<ProductsPOCO> resultList = new List<ProductsPOCO>();

            while (iterator.HasMoreResults) {
                var documents = await iterator.ReadNextAsync();
                resultList.AddRange(documents); // Add results to the list
            }

            if (resultList.Count > 0) {
                _logger.LogInformation($"Returning {resultList.Count} products.");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(resultList); // Serialize and return the list
                return response;
            }

            return req.CreateResponse(HttpStatusCode.NoContent); // Return 204 if no data is found
        }  
    }
}
