using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;

namespace com.appix.ai.design {
    public class MerchCreate {
        private readonly ILogger<MerchCreate> _logger;

        public MerchCreate(ILogger<MerchCreate> logger) {
            _logger = logger;
        }

        [Function(nameof(MerchCreateItem))]
        public async Task<HttpResponseData> MerchCreateItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "merch")] HttpRequestData req,
            [CosmosDBInput(Connection = "CosmosDbConnectionString")] CosmosClient client) {
            
            _logger.LogInformation("ITR..MerchCreateItem(): Function processed a request to create a merch item.");

            try {
                // Read the request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                
                if (string.IsNullOrWhiteSpace(requestBody)) {
                    _logger.LogWarning("Request body is empty.");
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Request body cannot be empty.");
                    return badRequestResponse;
                }

                _logger.LogInformation("ITR.. about to deserialize");
                // Deserialize the request body to MerchPOCO
                var merchItem = JsonSerializer.Deserialize<MerchPOCO>(requestBody, new JsonSerializerOptions {
                    PropertyNameCaseInsensitive = true
                });

                if (merchItem == null) {
                    _logger.LogWarning("Failed to deserialize request body to MerchPOCO.");
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Invalid request body format.");
                    return badRequestResponse;
                } else{
                    _logger.LogInformation("ITR.. merch item not null");
                }

                // Generate UUID for id if not provided
                if (string.IsNullOrWhiteSpace(merchItem.id)) {
                    merchItem.id = Guid.NewGuid().ToString();
                    _logger.LogInformation($"Generated UUID for merch item: {merchItem.id}");
                }

                // Set partition key to match Cosmos DB container configuration
                // The partition key path is /type
                merchItem.type = Constants.PARTITION_VALUE;
                
                // Get the container
                var container = client.GetContainer(Constants.DATABASE_NAME, Constants.TABLE_MERCH);

                // Create the item in Cosmos DB
                var response = await container.CreateItemAsync(
                    merchItem,
                    new PartitionKey(Constants.PARTITION_VALUE)
                );

                _logger.LogInformation($"Successfully created merch item with id: {merchItem.id}");

                // Return the created item
                var httpResponse = req.CreateResponse(HttpStatusCode.Created);
                await httpResponse.WriteAsJsonAsync(response.Resource);
                return httpResponse;

            } catch (CosmosException ex) {
                _logger.LogError(ex, "Cosmos DB error occurred while creating merch item.");
                
                if (ex.StatusCode == HttpStatusCode.Conflict) {
                    var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
                    await conflictResponse.WriteStringAsync($"Merch item with id already exists.");
                    return conflictResponse;
                }

                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"An error occurred while creating the merch item: {ex.Message}");
                return errorResponse;
            } catch (Exception ex) {
                _logger.LogError(ex, "An unexpected error occurred while creating merch item.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"An unexpected error occurred: {ex.Message}");
                return errorResponse;
            }
        }
    }
}

