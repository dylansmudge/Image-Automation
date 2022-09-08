using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;

namespace Image
{
    public class ImageAutomation
    {

        Uri URL =  new Uri("https://datafabric.coke.com");
        string APIKey = Environment.GetEnvironmentVariable("APIKey");

        private readonly HttpClient client = new HttpClient();


        [FunctionName("PostImage")]
        public async Task<IActionResult> PostImage(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
             // Call asynchronous network methods in a try/catch block to handle exceptions.
            try	
            {
                client.BaseAddress = URL;
                client.DefaultRequestHeaders.Add("x-api-key", APIKey);

                var content = await new StreamReader(req.Body).ReadToEndAsync();
                StringContent stringContent = new StringContent(content);
                HttpResponseMessage responseMessage = await client.PostAsync(URL, stringContent);

                string responseBody = await responseMessage.Content.ReadAsStringAsync();
                Console.WriteLine(responseBody);
                return new OkObjectResult(responseBody);

            }
            catch(HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");	
                Console.WriteLine("Message :{0} ",e.Message);
                return new BadRequestObjectResult(e.Message);
            }
            
        }
    }
}
