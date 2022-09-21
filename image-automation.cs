using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Images
{
    public class ImageAutomation
    {
        Uri URL = new Uri("https://datafabric.coke.com");
        string APIKey = Environment.GetEnvironmentVariable("APIKey");
        private readonly DataFabricManager _dataFabricManager;

        public ImageAutomation(DataFabricManager dataFabricManager)
        {
            _dataFabricManager = dataFabricManager;
        }

        [FunctionName("DurableFunctionsOrchestrationCSharp1")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();
            string input = context.GetInput<string>();
            try
            {
                outputs.Add(await context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp1_Hello", input));
            }
            catch (FunctionFailedException e)
            {
                Console.WriteLine(e);
            }
            return outputs;

        }

        [FunctionName("DurableFunctionsOrchestrationCSharp1_Hello")]
        public async Task<string> SayHelloAsync([ActivityTrigger] string content, ILogger log)
        {
            HttpClient client = new HttpClient();
            List<String> imagesList = new List<String>();
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                if (client.BaseAddress == null)
                {
                    client.BaseAddress = URL;
                }
                client.DefaultRequestHeaders.Add("x-api-key", APIKey);
                await _dataFabricManager.dataFabricPaging(content);
                return $"Hello {content}!";
            }
            catch (HttpRequestException e)
            {
                log.LogError("\nException Caught!");
                log.LogError("Message :{0} ", e.Message);
                return $"Hello {content}!";
            }
            
        }

        /*
        HTTP request to POST and download the photos. Requires the datafabric class for 
        the logic.
        */

        [FunctionName("PostImage")]
        public async Task<IActionResult> PostImage(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            HttpClient client = new HttpClient();
            List<String> imagesList = new List<String>();
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                if (client.BaseAddress == null)
                {
                    client.BaseAddress = URL;
                }
                client.DefaultRequestHeaders.Add("x-api-key", APIKey);
                //Read contents of API callback
                string content = await new StreamReader(req.Body).ReadToEndAsync();
                await _dataFabricManager.dataFabricPaging(content);
                return new OkObjectResult(content);
            }
            catch (HttpRequestException e)
            {
                log.LogError("\nException Caught!");
                log.LogError("Message :{0} ", e.Message);
                return new BadRequestObjectResult(e.Message);
            }
        }

        [FunctionName("DurableFunctionsOrchestrationCSharp1_HttpStart")]
        public static async Task<IActionResult> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string content = await new StreamReader(req.Body).ReadToEndAsync();
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("DurableFunctionsOrchestrationCSharp1",null, content);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
