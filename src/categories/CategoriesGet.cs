using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using System.Linq;

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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "categories/all")] HttpRequestData req,
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

        [Function(nameof(CategoriesFilter))]
        public async Task<HttpResponseData> CategoriesFilter(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "categories")] HttpRequestData req,
            [CosmosDBInput(Connection = "CosmosDbConnectionString")] CosmosClient client) {
            _logger.LogInformation("ITR..CategoriesFilter(): Function processed a request with optional filters.");

            // Parse optional query parameters
            var queryParams = req.Query;
            var queryParts = new List<string>();
            var parameters = new Dictionary<string, object>();

            // Handle isActive filter
            // Default to true if not provided, otherwise use whatever is sent
            bool isActive = true;
            if (!string.IsNullOrEmpty(queryParams["isActive"]) && bool.TryParse(queryParams["isActive"], out bool isActiveParam)) {
                isActive = isActiveParam;
            }

            // Filter by isActive value (defaults to true if not provided)
            queryParts.Add("c.isActive = @isActive");
            parameters["@isActive"] = isActive;

            // Build the query with WHERE clause and ORDER BY
            var whereClause = "WHERE " + string.Join(" AND ", queryParts);
            var queryDefinition = new QueryDefinition($"SELECT * FROM c {whereClause} ORDER BY c.sort DESC");
            
            // Add all parameters
            foreach (var param in parameters) {
                queryDefinition = queryDefinition.WithParameter(param.Key, param.Value);
            }

            var container = client.GetContainer(Constants.DATABASE_NAME, Constants.TABLE_CATEGORIES);
            var iterator = container.GetItemQueryIterator<CategoriesPOCO>(queryDefinition);

            List<CategoriesPOCO> resultList = new List<CategoriesPOCO>();

            while (iterator.HasMoreResults) {
                var documents = await iterator.ReadNextAsync();
                resultList.AddRange(documents);
            }

            // Additional sort in-memory as backup (though CosmosDB should handle it)
            resultList = resultList.OrderByDescending(x => x.sort).ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);

            if (resultList.Count > 0) {
                _logger.LogInformation($"Returning {resultList.Count} categories with applied filters.");
                await response.WriteAsJsonAsync(resultList);
            } else {
                _logger.LogInformation("No categories found with applied filters. Returning empty array.");
                await response.WriteAsJsonAsync(new List<CategoriesPOCO>());
            }

            return response;
        }
    }
}
