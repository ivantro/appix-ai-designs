using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using System.Linq;

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

        [Function(nameof(MerchFilter))]
        public async Task<HttpResponseData> MerchFilter(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "merch/filter")] HttpRequestData req,
            [CosmosDBInput(Connection = "CosmosDbConnectionString")] CosmosClient client) {
            _logger.LogInformation("ITR..MerchFilter(): Function processed a request with optional filters.");

            // Parse optional query parameters
            var queryParams = req.Query;
            var queryParts = new List<string>();
            var parameters = new Dictionary<string, object>();

            // Support categoryIds as comma-separated list (e.g., categoryIds=1,2,3)
            var categoryIdsParam = queryParams["categoryIds"];
            if (!string.IsNullOrEmpty(categoryIdsParam)) {
                var categoryIdsString = categoryIdsParam.ToString();
                if (!string.IsNullOrEmpty(categoryIdsString)) {
                    var categoryIdValues = categoryIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => id.Trim())
                        .Where(id => int.TryParse(id, out _))
                        .Select(int.Parse)
                        .ToList();

                    if (categoryIdValues.Count > 0) {
                        var categoryIdConditions = new List<string>();
                        for (int i = 0; i < categoryIdValues.Count; i++) {
                            var paramName = $"@categoryId{i}";
                            categoryIdConditions.Add($"ARRAY_CONTAINS(c.categoryIds, {paramName})");
                            parameters[paramName] = categoryIdValues[i];
                        }
                        queryParts.Add($"({string.Join(" OR ", categoryIdConditions)})");
                    }
                }
            }
            // Also support single categoryId for backward compatibility
            else if (!string.IsNullOrEmpty(queryParams["categoryId"]) && int.TryParse(queryParams["categoryId"], out int categoryId)) {
                queryParts.Add("ARRAY_CONTAINS(c.categoryIds, @categoryId)");
                parameters["@categoryId"] = categoryId;
            }

            if (!string.IsNullOrEmpty(queryParams["productId"]) && int.TryParse(queryParams["productId"], out int productId)) {
                queryParts.Add("c.productId = @productId");
                parameters["@productId"] = productId;
            }

            if (!string.IsNullOrEmpty(queryParams["isMine"]) && bool.TryParse(queryParams["isMine"], out bool isMine)) {
                queryParts.Add("c.isMine = @isMine");
                parameters["@isMine"] = isMine;
            }

            if (!string.IsNullOrEmpty(queryParams["isTopCategory"]) && bool.TryParse(queryParams["isTopCategory"], out bool isTopCategory)) {
                queryParts.Add("c.isTopCategory = @isTopCategory");
                parameters["@isTopCategory"] = isTopCategory;
            }

            if (!string.IsNullOrEmpty(queryParams["isNewCategory"]) && bool.TryParse(queryParams["isNewCategory"], out bool isNewCategory)) {
                queryParts.Add("c.isNewCategory = @isNewCategory");
                parameters["@isNewCategory"] = isNewCategory;
            }

            bool isActive = true;
            if (!string.IsNullOrEmpty(queryParams["isActive"]) && bool.TryParse(queryParams["isActive"], out bool isActiveParam)) {
                isActive = isActiveParam;
            }
            queryParts.Add("c.isActive = @isActive");
            parameters["@isActive"] = isActive;

            // Build the final query
            QueryDefinition queryDefinition;
            if (queryParts.Count > 0) {
                var whereClause = "WHERE " + string.Join(" AND ", queryParts);
                queryDefinition = new QueryDefinition($"SELECT * FROM c {whereClause}");
                
                // Add all parameters
                foreach (var param in parameters) {
                    queryDefinition = queryDefinition.WithParameter(param.Key, param.Value);
                }
            } else {
                queryDefinition = new QueryDefinition("SELECT * FROM c");
            }

            var container = client.GetContainer(Constants.DATABASE_NAME, Constants.TABLE_MERCH);
            var iterator = container.GetItemQueryIterator<MerchPOCO>(queryDefinition);

            List<MerchPOCO> resultList = new List<MerchPOCO>();

            while (iterator.HasMoreResults) {
                var documents = await iterator.ReadNextAsync();
                resultList.AddRange(documents);
            }

            if (resultList.Count > 0) {
                _logger.LogInformation($"Returning {resultList.Count} merch items with applied filters.");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(resultList);
                return response;
            }

            return req.CreateResponse(HttpStatusCode.NoContent);
        }  
    }
}
