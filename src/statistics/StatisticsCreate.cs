using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;

namespace com.appix.ai.design {
    public class StatisticsCreate {
        private readonly ILogger<StatisticsCreate> _logger;

        public StatisticsCreate(ILogger<StatisticsCreate> logger) {
            _logger = logger;
        }

        [Function(nameof(RecordClick))]
        public async Task<HttpResponseData> RecordClick(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "statistics/clicks")] HttpRequestData req,
            [CosmosDBInput(Connection = "CosmosDbConnectionString")] CosmosClient client) {
            
            _logger.LogInformation("ITR..RecordClick(): Function processed a request to record a click.");

            try {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                
                if (string.IsNullOrWhiteSpace(requestBody)) {
                    _logger.LogWarning("Request body is empty.");
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Request body cannot be empty.");
                    return badRequestResponse;
                }

                var click = JsonSerializer.Deserialize<ClickPOCO>(requestBody, new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true
                });

                if (click == null) {
                    _logger.LogWarning("Failed to deserialize request body to ClickPOCO.");
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Invalid request body format.");
                    return badRequestResponse;
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(click.senderId) || click.productId <= 0) {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("senderId and productId are required.");
                    return badRequestResponse;
                }

                // Generate UUID for id if not provided
                if (string.IsNullOrWhiteSpace(click.id)) {
                    click.id = Guid.NewGuid().ToString();
                }

                // Set partition key and timestamp
                click.type = Constants.PARTITION_VALUE;
                if (click.timestamp == default(DateTime)) {
                    click.timestamp = DateTime.UtcNow;
                }

                var clicksContainer = client.GetContainer(Constants.DATABASE_NAME, Constants.TABLE_CLICKS);

                // Create the click record
                var clickResponse = await clicksContainer.CreateItemAsync(
                    click,
                    new PartitionKey(Constants.PARTITION_VALUE)
                );

                _logger.LogInformation($"Successfully recorded click with id: {click.id} for productId: {click.productId}");

                // Update or create the stats record
                await UpdateClickStats(client, click.productId);

                var httpResponse = req.CreateResponse(HttpStatusCode.Created);
                await httpResponse.WriteAsJsonAsync(clickResponse.Resource);
                return httpResponse;

            } catch (CosmosException ex) {
                _logger.LogError(ex, "Cosmos DB error occurred while recording click.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"An error occurred: {ex.Message}");
                return errorResponse;
            } catch (Exception ex) {
                _logger.LogError(ex, "An unexpected error occurred while recording click.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"An unexpected error occurred: {ex.Message}");
                return errorResponse;
            }
        }

        private async Task UpdateClickStats(CosmosClient client, int productId) {
            try {
                var statsContainer = client.GetContainer(Constants.DATABASE_NAME, Constants.TABLE_CLICK_STATS);
                var productIdString = productId.ToString();

                // Try to read existing stats
                try {
                    var existingStats = await statsContainer.ReadItemAsync<ClickStatsPOCO>(
                        productIdString,
                        new PartitionKey(Constants.PARTITION_VALUE)
                    );

                    // Increment count
                    existingStats.Resource.count++;
                    await statsContainer.ReplaceItemAsync(
                        existingStats.Resource,
                        productIdString,
                        new PartitionKey(Constants.PARTITION_VALUE)
                    );
                } catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) {
                    // Create new stats record
                    var newStats = new ClickStatsPOCO {
                        id = productIdString,
                        type = Constants.PARTITION_VALUE,
                        productId = productId,
                        count = 1
                    };
                    await statsContainer.CreateItemAsync(
                        newStats,
                        new PartitionKey(Constants.PARTITION_VALUE)
                    );
                }
            } catch (Exception ex) {
                _logger.LogError(ex, $"Failed to update stats for productId: {productId}");
                // Don't throw - we don't want to fail the click recording if stats update fails
            }
        }
    }
}

