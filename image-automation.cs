using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Collections.Generic;

namespace Image
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

        [FunctionName("PostImage")]
        public IActionResult PostImage(
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
                string content = new StreamReader(req.Body).ReadToEnd();
                _dataFabricManager.dataFabricPaging(content);
                return new NoContentResult();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return new BadRequestObjectResult(e.Message);
            }
        }
    }
}
