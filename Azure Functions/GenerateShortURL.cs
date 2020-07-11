using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Linq;

namespace cURL_Function
{
    public static class GenerateURL
    {
        [FunctionName("GenerateShortURL")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "<database name>",
                collectionName: "<collection name>",
                ConnectionStringSetting = "CosmosDBConnection")]IAsyncCollector<GeneratedResult> cosmosItem,
            ILogger log)
        {
            log.LogInformation("Received request to generate short URL");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<urlBody>(requestBody);

            //if no customize alias
            if(string.IsNullOrEmpty(input.alias))
            {
                bool Exist = true;
                GeneratedResult response = new GeneratedResult();
                //check for duplication for random generated alias
                while (Exist)
                {
                    string shorten = RandomGenerator();
                    response = new GeneratedResult
                    {
                        url = input.url,
                        shorturl = shorten,
                        dateGenerated = DateTime.UtcNow.ToShortDateString()
                    };
                    Exist = await ShortenExistAsync(shorten);
                }

                //create item in cosmos DB
                await cosmosItem.AddAsync(response);
                //return json object
                return new OkObjectResult(JsonConvert.SerializeObject(response));
            }
            else
            {
                //check if custom alias existed
                bool Exist = await ShortenExistAsync(input.alias);
                if(Exist)
                {
                    //if existed, simply response "existed"
                    return new OkObjectResult("existed");
                }
                else
                {
                    //otherwise create a new item in cosmos DB with custom alias
                    GeneratedResult response = new GeneratedResult()
                    {
                        url = input.url,
                        shorturl = input.alias,
                        dateGenerated = DateTime.UtcNow.ToShortDateString()
                    };

                    await cosmosItem.AddAsync(response);

                    return new OkObjectResult("created");
                }
            }
        }

        //random string generator, for custom alias
        private static string RandomGenerator()
        {
            Random random = new Random(Guid.NewGuid().GetHashCode());
            int length = random.Next(6, 9);
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        //check if short url existed
        private static async Task<bool> ShortenExistAsync(string input)
        {
            //create endpoint
            string cosmos_endpoint = Environment.GetEnvironmentVariable("cosmos_endpoint");
            string cosmos_key = Environment.GetEnvironmentVariable("cosmos_key");
            CosmosClient cosmosClient = new CosmosClient(cosmos_endpoint, cosmos_key);
            Container container = cosmosClient.GetContainer("timestamp", "url");

            //query against cosmos DB
            var query = string.Format("SELECT c.url,c.shorturl,c.dateGenerated FROM c WHERE c.shorturl = '{0}'", input);
            QueryDefinition queryDefinition = new QueryDefinition(query);
            FeedIterator<GeneratedResult> queryResultSetIterator = container.GetItemQueryIterator<GeneratedResult>(queryDefinition);

            //check number of records
            int count = 0;
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<GeneratedResult> currentRecord = await queryResultSetIterator.ReadNextAsync();
                count = currentRecord.Count();
            }

            if (count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class urlBody
    {
       public string url { get; set; }
        public string? alias { get; set; }
    }

    public class GeneratedResult
    {
        public string url { get; set; }
        public string shorturl { get; set; }
        public string dateGenerated { get; set; }
    }
}
