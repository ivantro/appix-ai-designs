using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;

namespace com.appix.ai.design {
    public class StatisticsGet {
        private readonly ILogger<StatisticsGet> _logger;

        public StatisticsGet(ILogger<StatisticsGet> logger) {
            _logger = logger;
        }

        [Function(nameof(GetClickStats))]
        public async Task<HttpResponseData> GetClickStats(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "statistics/stats/{productId?}")] HttpRequestData req,
            [CosmosDBInput(Connection = "CosmosDbConnectionString")] CosmosClient client,
            string? productId) {
            
            _logger.LogInformation($"ITR..GetClickStats(): Function processed a request for productId: {productId ?? "all"}");

            try {
                var statsContainer = client.GetContainer(Constants.DATABASE_NAME, Constants.TABLE_CLICK_STATS);

                if (!string.IsNullOrEmpty(productId)) {
                    // Get stats for a specific product
                    try {
                        var stats = await statsContainer.ReadItemAsync<ClickStatsPOCO>(
                            productId,
                            new PartitionKey(Constants.PARTITION_VALUE)
                        );

                        var response = req.CreateResponse(HttpStatusCode.OK);
                        await response.WriteAsJsonAsync(stats.Resource);
                        return response;
                    } catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
                        // Return zero count if not found
                        var zeroStats = new ClickStatsPOCO {
                            id = productId,
                            productId = productId,
                            count = 0
                        };
                        var response = req.CreateResponse(HttpStatusCode.OK);
                        await response.WriteAsJsonAsync(zeroStats);
                        return response;
                    }
                } else {
                    // Get all stats
                    var queryDefinition = new QueryDefinition("SELECT * FROM c");
                    var iterator = statsContainer.GetItemQueryIterator<ClickStatsPOCO>(queryDefinition);

                    List<ClickStatsPOCO> resultList = new List<ClickStatsPOCO>();

                    while (iterator.HasMoreResults) {
                        var documents = await iterator.ReadNextAsync();
                        resultList.AddRange(documents);
                    }

                    var response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(resultList);
                    return response;
                }
            } catch (Exception ex) {
                _logger.LogError(ex, "An error occurred while retrieving click stats.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"An error occurred: {ex.Message}");
                return errorResponse;
            }
        }

        [Function(nameof(GetClicks))]
        public async Task<HttpResponseData> GetClicks(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "statistics/clicks")] HttpRequestData req,
            [CosmosDBInput(Connection = "CosmosDbConnectionString")] CosmosClient client) {
            
            _logger.LogInformation("ITR..GetClicks(): Function processed a request to retrieve clicks.");

            var queryParams = req.Query;
            var queryParts = new List<string>();
            var parameters = new Dictionary<string, object>();

            // Filter by productId if provided
            if (!string.IsNullOrEmpty(queryParams["productId"])) {
                queryParts.Add("c.productId = @productId");
                parameters["@productId"] = queryParams["productId"].ToString();
            }

            // Filter by senderId if provided
            if (!string.IsNullOrEmpty(queryParams["senderId"])) {
                queryParts.Add("c.senderId = @senderId");
                parameters["@senderId"] = queryParams["senderId"].ToString();
            }

            // Filter by tag if provided
            if (!string.IsNullOrEmpty(queryParams["tag"])) {
                queryParts.Add("c.tag = @tag");
                parameters["@tag"] = queryParams["tag"].ToString();
            }

            var clicksContainer = client.GetContainer(Constants.DATABASE_NAME, Constants.TABLE_CLICKS);
            
            QueryDefinition queryDefinition;
            if (queryParts.Count > 0) {
                var whereClause = "WHERE " + string.Join(" AND ", queryParts);
                queryDefinition = new QueryDefinition($"SELECT * FROM c {whereClause} ORDER BY c.timestamp DESC");
                
                foreach (var param in parameters) {
                    queryDefinition = queryDefinition.WithParameter(param.Key, param.Value);
                }
            } else {
                queryDefinition = new QueryDefinition("SELECT * FROM c ORDER BY c.timestamp DESC");
            }

            var iterator = clicksContainer.GetItemQueryIterator<ClickPOCO>(queryDefinition);
            List<ClickPOCO> resultList = new List<ClickPOCO>();

            while (iterator.HasMoreResults) {
                var documents = await iterator.ReadNextAsync();
                resultList.AddRange(documents);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(resultList);
            return response;
        }
    }
}

