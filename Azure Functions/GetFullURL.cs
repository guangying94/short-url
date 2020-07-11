using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using System.Linq;

namespace cURL_Function
{
    public static class GetFullURL
    {
        [FunctionName("GetFullURL")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Received request to get full URL");

            //query parameter is "alias"
            string alias = req.Query["alias"];
            string fullUrl = await GetFullURLAsync(alias);

            return new OkObjectResult(fullUrl);
        }

        private static async Task<string> GetFullURLAsync(string input)
        {
            //create cosmos DB client
            string cosmos_endpoint = Environment.GetEnvironmentVariable("cosmos_endpoint");
            string cosmos_key = Environment.GetEnvironmentVariable("cosmos_key");
            CosmosClient cosmosClient = new CosmosClient(cosmos_endpoint, cosmos_key);
            Container container = cosmosClient.GetContainer("timestamp", "url");

            //query cosmos DB by alias
            var query = string.Format("SELECT c.url,c.shorturl,c.dateGenerated FROM c WHERE c.shorturl = '{0}'", input);
            QueryDefinition queryDefinition = new QueryDefinition(query);
            FeedIterator<GeneratedResult> queryResultSetIterator = container.GetItemQueryIterator<GeneratedResult>(queryDefinition);
            string fullURL = "";

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<GeneratedResult> currentRecord = await queryResultSetIterator.ReadNextAsync();
                foreach(GeneratedResult item in currentRecord)
                {
                    fullURL = item.url;
                }
            }

            return fullURL;
        }
    }
}
