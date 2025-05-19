using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AIDesign;

// Usage in Javascript:
// fetch("https://<your-function>.azurewebsites.net/api/ProxyFunction?url=https://external-api.com/data")

public class ProxyFunction {
    private static readonly HttpClient httpClient = new HttpClient();

    [Function("ProxyFunction")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(Microsoft.Azure.Functions.Worker.AuthorizationLevel.Anonymous, "get", "post", "options")] HttpRequestData req){
        // Handle preflight
        if (req.Method == "OPTIONS"){
            var response = req.CreateResponse(HttpStatusCode.NoContent);
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            return response;
        }

        string? targetUrl = req.Query["url"];

        if (string.IsNullOrEmpty(targetUrl)) {
            // Try reading from the body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(requestBody);
            if (data.TryGetProperty("url", out var urlElement)) {
                targetUrl = urlElement.GetString();
            }
        }

        if (string.IsNullOrEmpty(targetUrl)){
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteStringAsync("Missing 'url' parameter.");
            return response;
        }

        try {
            var proxyRequest = new HttpRequestMessage(new HttpMethod(req.Method), targetUrl);

            // Optionally: copy headers or body
            if (req.Method == "POST" || req.Method == "PUT") {
                proxyRequest.Content = new StreamContent(req.Body);
                if (req.Headers.TryGetValues("Content-Type", out var contentType)) {
                    proxyRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType.First());
                }
            }

            HttpResponseMessage response = await httpClient.SendAsync(proxyRequest);
            string responseContent = await response.Content.ReadAsStringAsync();

            var result = req.CreateResponse((HttpStatusCode)response.StatusCode);
            await result.WriteStringAsync(responseContent);
            
            if (response.Content.Headers.ContentType != null) {
                result.Headers.Add("Content-Type", response.Content.Headers.ContentType.ToString());
            }

            // Add CORS headers
            result.Headers.Add("Access-Control-Allow-Origin", "*");
            result.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
            result.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            return result;
        } catch (HttpRequestException ex) {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Proxy error: {ex.Message}");
            return response;
        }
    }
}