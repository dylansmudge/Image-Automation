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
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage;
using System.Net;
using System.Drawing;
using System.Text;

namespace Images
{
    public class ImageAutomation
    {
        Uri URL = new Uri("https://datafabric.coke.com");
        string APIKey = Environment.GetEnvironmentVariable("APIKey");
        string Uri = Environment.GetEnvironmentVariable("BlobUri");
        string AccountName = Environment.GetEnvironmentVariable("AccountName");
        string AccountKey = Environment.GetEnvironmentVariable("AccountKey");
        private readonly BlobContainerClient _photoBlobContainerClient;
        private readonly BlobClient _photoBlobClient;
        private readonly DataFabricManager _dataFabricManager;

        public ImageAutomation(DataFabricManager dataFabricManager)
        {
            _dataFabricManager = dataFabricManager;

            StorageSharedKeyCredential storageSharedKeyCredential = 
            new StorageSharedKeyCredential(AccountName, AccountKey);

            this._photoBlobContainerClient = 
            new BlobContainerClient(new Uri(Uri), storageSharedKeyCredential);

            this._photoBlobClient = 
            new BlobClient(new Uri(Uri), storageSharedKeyCredential);
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
                return new NoContentResult();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return new BadRequestObjectResult(e.Message);
            }
        }

        [FunctionName("PostOneImage")]
        public async Task<IActionResult> PostOneImage(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                Uri imagelink = new Uri("https://tccc-f5-tenant1-cdnep02.azureedge.net/api/public/content/00049000054828_A1N1");
                
                HttpClient httpClient = new HttpClient();
                httpClient.BaseAddress = URL;
                httpClient.DefaultRequestHeaders.Add("x-api-key", APIKey);
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                Stream stream = await httpClient.GetStreamAsync(imagelink).ConfigureAwait(false); 

                StreamReader streamReader = new StreamReader(stream);
                
                string imageString = streamReader.ReadToEnd();
                int streamLength = imageString.Length;

                MemoryStream memoryStream = new MemoryStream();
                //StreamWriter streamWriter = new StreamWriter(memoryStream);
                //streamWriter.Write(imageString);

                
                //BinaryReader reader = new BinaryReader(stream);
                byte[] byteArray = Encoding.ASCII.GetBytes(imageString);
                memoryStream.Write(byteArray);
                //reader.Read(buffer, 0, buffer.Length);

                //streamWriter.Flush();
                memoryStream.Position = 0;
                
                //BinaryData binaryData = new BinaryData(buffer);
                //Stream image = binaryData.ToStream();

                //Read contents of API callback
                BlobClient blobClient =  _photoBlobContainerClient.GetBlobClient("144706.jpg");
                BlobHttpHeaders blobHttpHeader = new BlobHttpHeaders();
                blobHttpHeader.ContentType = "image/jpg";
                await _photoBlobClient.UploadAsync(memoryStream);
                return new OkObjectResult(stream);
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
