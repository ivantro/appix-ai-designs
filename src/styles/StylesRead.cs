// using System.Collections.Generic;
// using Microsoft.Extensions.Logging;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Azure.Functions.Worker;
// using System.Web;
// using System.Net;
// using Microsoft.Azure.Cosmos;
// using Microsoft.Azure.Functions.Worker.Http;
// using System.Text.Json;
// using System.Diagnostics;
// using System.Text;
// using Newtonsoft.Json;

// namespace com.appix.ai.design{
//     public class Style
//     {
//         public required string id { get; set; }
//         // Add other properties as per your CosmosDB document schema
//     }

//     public class StylesRead{
//         private readonly ILogger<StylesRead> _logger;
//         private readonly string _connectionString;
//         private readonly string _databaseId = "ai-designs"; // Change if your DB name is different
//         private readonly string _containerId = "styles";

//         public StylesRead(ILogger<StylesRead> logger){
//             _logger = logger;
//             _connectionString = Environment.GetEnvironmentVariable("CosmosDbConnectionString");
//         }

//         [Function("StylesRead")]
//         public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req){
//             _logger.LogInformation("StylesRead function processed a request.");

//             var cosmosClient = new CosmosClient(_connectionString);
//             var container = cosmosClient.GetContainer(_databaseId, _containerId);

//             var query = "SELECT * FROM c";
//             var iterator = container.GetItemQueryIterator<Style>(query);
//             var results = new List<Style>();

//             while (iterator.HasMoreResults){
//                 var response = await iterator.ReadNextAsync();
//                 results.AddRange(response.ToList());
//             }

//             return new OkObjectResult(results);
//         }
//     }
// }
