using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Net;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage;
using System.IO;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;

namespace Images
{
    public class DataFabricManager
    {
        Uri URL = new Uri("https://datafabric.coke.com");
        string APIKey = Environment.GetEnvironmentVariable("APIKey");
        string Uri = Environment.GetEnvironmentVariable("BlobUri");
        string AccountName = Environment.GetEnvironmentVariable("AccountName");
        string AccountKey = Environment.GetEnvironmentVariable("AccountKey");
        private readonly BlobContainerClient _photoBlobContainerClient;
        private readonly BlobClient _photoBlobClient;
        private readonly HttpClient client = new HttpClient();
        private readonly ILogger log;

        /*
        Initialise the base address and headers. 
        As we are making multiple GraphQL queries this is essential.
        */
        public DataFabricManager(ILogger<DataFabricManager> logger)
        {
            client.BaseAddress = URL;
            client.DefaultRequestHeaders.Add("x-api-key", APIKey);

            StorageSharedKeyCredential storageSharedKeyCredential =
            new StorageSharedKeyCredential(AccountName, AccountKey);

            this._photoBlobContainerClient =
            new BlobContainerClient(new Uri(Uri), storageSharedKeyCredential);

            this._photoBlobClient =
            new BlobClient(new Uri(Uri), storageSharedKeyCredential);

            this.log = logger;

        }



        /*
        The main logic behind the query. We want to download the images with the Golden Id and then
        return the next token for sequential queries.
        */
        public async Task<String> dataFabricQuery(String content, string after = "")
        {
            List<String> imagesList = new List<String>();
            // Call asynchronous network methods in a try/catch block to handle exceptions.


            StringContent stringContent = new StringContent(content);

            //Synchronous call of client POST
            var task = Task.Run(() => client.PostAsync(URL, stringContent));
            task.Wait();
            HttpResponseMessage responseMessage = task.Result;

            //Synchronous call of response Read
            var resTask = Task.Run(() => responseMessage.Content.ReadAsStringAsync());
            resTask.Wait();
            string responseBody = resTask.Result;

            List<String> goldenRecordNumberMmrIdList = new List<String>();

            //Deserialize the response body so that we can identify items uniquely.
            Root root = JsonConvert.DeserializeObject<Root>(responseBody);
            List<String> errors = new List<String>();
            try
            {
                log.LogInformation("Beginning parse");
                if (root.data != null)
                {
                    foreach (var items in root.data.getMMRList.items)
                    {
                        if (items.goldenRecordNumberMmrId != null && items != null)
                        {
                            await dataFabricItemParse(items, imagesList);
                        }
                    }

                }
                else
                {
                    after = root.data.getMMRList.nextToken;
                    return after;
                }
            }
            catch (NullReferenceException e)
            {
                log.LogWarning("Request failed: {e}", e);
            }
            //Catch any errors in our own API call
            catch
            {

                foreach (var err in root.errors)
                {
                    errors.Add(err.ToString());
                }

            }
            after = root.data.getMMRList.nextToken;
            return after;
        }

        /*
        Function used to paginate through the datafabric. 
        Runs through while the token is equal to something. 
        */
        public async Task dataFabricPaging(String content)
        {
            string token = "";
            int count = 0;
            Stopwatch stopwatch = new Stopwatch();
            while (token != null)
            {
                /*
                On the first iteration, the token is equal to an empty string, 
                hence the if-else statement. 
                */
                if (token == "")
                {

                    stopwatch.Start();
                    token = await dataFabricQuery(content);
                    count++;
                    log.LogInformation("Number of 1-Count calls: " + count);
                    log.LogInformation("Time taken: " + stopwatch.Elapsed.TotalSeconds);
                }
                else
                {
                    string replacement = "after: " + "\\\"" + token + "\\\"";
                    string output = "{\"query\":\"{\\n  getMMRList(count: 1, " + replacement + ") {\\n    items {\\n      goldenRecordNumberMmrId\\n      pgr {\\n        upc {\\n          images {\\n            type\\n            uniformResourceIdentifier\\n            fileEffectiveStartDate\\n          }\\n          itemReferences {\\n            referencedItem\\n            referencedUPC {\\n              images {\\n                type\\n                fileEffectiveStartDate\\n                uniformResourceIdentifier\\n              }\\n            }\\n          }\\n        }\\n      }\\n    }\\n    nextToken\\n  }\\n}\"}";
                    token = await dataFabricQuery(output);
                    count++;
                    log.LogInformation("Token is" + token);
                    log.LogInformation("Number of 1-Count calls: " + count);
                    log.LogInformation("Time taken: " + stopwatch.Elapsed.TotalSeconds);
                }
            }
        }



        public async Task readType(Items items, ItemReference itemReference, List<String> imagesList)
        {

            foreach (var image in itemReference.referencedUPC.images)
            {
                if (image.type.Contains("A1N1"))
                {
                    imagesList.Add(image.uniformResourceIdentifier);
                    string fileName = items.goldenRecordNumberMmrId + ".jpg";
                    string inputPath = Path.Combine(Path.GetTempPath(), fileName);
                    string outputPath = "/Users/dylancarlyle/Pictures/Temp/" + fileName;
                    try
                    {
                        downloadImage(image.uniformResourceIdentifier, inputPath);
                    }
                    //Catch any errors from the datafabric. 
                    //Note: The orignial API call can give us 404 errors and potentially other 400 errors.
                    catch (WebException ex)
                    {
                        log.LogInformation("ex is {ex}", ex.ToString());
                        break;
                    }
                    try
                    {
                        //Code to resize image
                        resizeImage(inputPath, outputPath);
                        //Code to read downloaded image to bytes
                        long imageToUploadSize = readImageByteSize(outputPath);
                        //Blobclient upload file
                        await uploadImageToBlobAsync(outputPath, fileName, imageToUploadSize);
                    }
                    catch (Azure.RequestFailedException e)
                    {
                        log.LogWarning("Request failed: {e}", e);
                        break;
                    }
                    catch (FileNotFoundException f)
                    {
                        log.LogWarning("File not found: {f}", f);
                        break;
                    }
                    catch (UnknownImageFormatException e)
                    {
                        log.LogWarning("Format not found: {e}", e);
                        break;
                    }
                }
                else
                {
                    foreach (var image1 in items.pgr.upc.images)
                    {
                        if (image1.type.Contains("A1N1"))
                        {
                            imagesList.Add(image1.uniformResourceIdentifier);
                            string fileName = items.goldenRecordNumberMmrId + ".jpg";
                            string inputPath = Path.Combine(Path.GetTempPath(), fileName);
                            string outputPath = Path.Combine(Path.GetTempPath() + fileName);
                            using (var downloadClient = new WebClient())
                                try
                                {
                                    downloadImage(image1.uniformResourceIdentifier, inputPath);
                                }
                                //Catch any errors from the datafabric. 
                                //Note: The orignial API call can give us 404 errors and potentially other 400 errors.
                                catch (WebException ex)
                                {
                                    log.LogWarning("Exception is {ex}", ex);
                                    break;
                                }
                            try
                            {
                                resizeImage(inputPath, outputPath);

                                //Code to read downloaded image to bytes
                                long imageSize = readImageByteSize(outputPath);

                                //Blobclient upload file
                                await uploadImageToBlobAsync(outputPath, fileName, imageSize);
                                BlobClient blobClient = _photoBlobContainerClient.GetBlobClient(fileName);

                            }
                            catch (Azure.RequestFailedException e)
                            {
                                log.LogWarning("Request failed: {e}", e);
                                break;
                            }
                            catch (FileNotFoundException f)
                            {
                                log.LogWarning("File not found: {f}", f);
                                break;
                            }
                            catch (UnknownImageFormatException e)
                            {
                                log.LogWarning("Format not found: {e}", e);
                                break;
                            }
                        }
                    }
                }
            }

        }


        public async Task dataFabricItemParse(Items items, List<String> imagesList)
        {
            if (items.pgr == null || items.pgr.upc == null)
            {
                log.LogInformation("UPC is null");
            }
            else
            {
                try
                {
                    foreach (var itemReference in items.pgr.upc.itemReferences)
                    {
                        await readType(items, itemReference, imagesList);
                    }

                }
                catch (System.NullReferenceException e)
                {
                    log.LogInformation("Item reference is is null: {e}", e);
                }


            }

        }
        public void resizeImage(String inputPath, string outputPath)
        {
        IImageFormat format;
            try
            {
                Image imageResize = Image.Load(inputPath, out format);
                imageResize.Mutate(x => x.Resize(600, 600));
                imageResize.SaveAsJpeg(outputPath);
            }
            catch (System.IO.FileLoadException e)
            {
                log.LogInformation("Exception occurred: " + e);
            }


        }

        public long readImageByteSize(string outputPath)
        {
            byte[] buffer = File.ReadAllBytes(outputPath);
            long byteSize = buffer.Length;
            return byteSize;
        }

        public async Task uploadImageToBlobAsync(String outputPath, String fileName, long imageToUploadSize)
        {
            BlobClient blobClient = _photoBlobContainerClient.GetBlobClient(fileName);
            if (!blobClient.Exists())
            {
                BlobHttpHeaders blobHttpHeader = new BlobHttpHeaders();
                blobHttpHeader.ContentType = "image/jpg";
                await blobClient.UploadAsync(outputPath, blobHttpHeader);
                log.LogInformation(" Uploaded new image with " + imageToUploadSize + " bytes.");
                Console.WriteLine(" Uploaded new image with " + imageToUploadSize + " bytes.");
            }
            else
            {
                var content = blobClient.DownloadContent();
                long blobImageSize = content.Value.Details.ContentLength;
                if (imageToUploadSize != blobImageSize)
                {
                    BlobHttpHeaders blobHttpHeader = new BlobHttpHeaders();
                    blobHttpHeader.ContentType = "image/jpg";
                    await blobClient.UploadAsync(outputPath, blobHttpHeader);
                    log.LogInformation(" Uploaded new image with " + imageToUploadSize + " bytes. \n Previous image had " + blobImageSize + " bytes.");
                    Console.WriteLine(" Uploaded new image with " + imageToUploadSize + " bytes. \n Previous image had " + blobImageSize + " bytes.");
                }
                else
                {
                    log.LogInformation("Nothing to upload");
                }
            }



        }

        public void downloadImage(String uniformResourceIdentifier, String inputPath)
        {
            using (var downloadClient = new WebClient())
                if (uniformResourceIdentifier != null)
                {
                    downloadClient.DownloadFile(new Uri(uniformResourceIdentifier), inputPath);

                }
        }
    } // End of Class
} //End of Namespace
