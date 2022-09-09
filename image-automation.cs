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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

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
            List<String> imagesList = new List<String>();
             // Call asynchronous network methods in a try/catch block to handle exceptions.
            try	
            {
                client.BaseAddress = URL;
                client.DefaultRequestHeaders.Add("x-api-key", APIKey);

                var content = await new StreamReader(req.Body).ReadToEndAsync();
                StringContent stringContent = new StringContent(content);
                HttpResponseMessage responseMessage = await client.PostAsync(URL, stringContent);
                string responseBody = await responseMessage.Content.ReadAsStringAsync();
                List<String> goldenRecordNumberMmrIdList = new List<String>();
                
                //Deserialize the response body so that we can identify items uniquely.
                Root root = JsonConvert.DeserializeObject<Root>(responseBody);
                string nextToken = root.data.getPGRList.nextToken;
                List<String> errors = new List<String>();
                try 
                {
                    foreach(var items in root.data.getPGRList.items)
                    {
                        foreach(var mmr in items.mmr)
                        {
                            if (mmr.goldenRecordNumberMmrId != null)
                            {
                                foreach(var image in items.upc.images)
                                {// Returns images of type A1N1 along with the URI of each
                                    if (image.type.Contains("A1N1") 
                                        && !goldenRecordNumberMmrIdList.Contains(mmr.goldenRecordNumberMmrId) 
                                        && !imagesList.Contains(image.uniformResourceIdentifier))
                                    {
                                        goldenRecordNumberMmrIdList.Add(mmr.goldenRecordNumberMmrId);
                                        imagesList.Add(image.type);
                                        imagesList.Add(image.uniformResourceIdentifier);
                                        imagesList.Add(mmr.goldenRecordNumberMmrId);
                                        Console.WriteLine("\n Image is of type: " + image.type);
                                        Console.WriteLine(" Is the image of type A1N1: " + image.type.Contains("A1N1"));
                                        Console.WriteLine(" Image is of uri: " + image.uniformResourceIdentifier);
                                        Console.WriteLine("Golden record number id is: " + mmr.goldenRecordNumberMmrId + "\n");
                                        string fileName = mmr.goldenRecordNumberMmrId + ".jpg";
                                        using (var downloadClient = new WebClient())
                                            {
                                                try 
                                                {
                                                    downloadClient.DownloadFile(new Uri(image.uniformResourceIdentifier), "/Users/dylancarlyle/Documents/Image Automation/Image-Automation/Downloaded Images/" + fileName);
                                                }
                                                catch (WebException ex)
                                                {
                                                    Console.WriteLine(ex);
                                                }
                                            }
                                    }
                                }
                            }
                        }
                    }
                }
                catch 
                {
                    foreach(var err in root.errors)
                    {
                        errors.Add(err.ToString());
                    }

                }
                Console.WriteLine("next token is " + nextToken);
                return new OkObjectResult(imagesList);
                //return new OkObjectResult("next token is " + nextToken);
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
