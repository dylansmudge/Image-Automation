using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace Image
{
    public class DataFabricManager
    {
        Uri URL = new Uri("https://datafabric.coke.com");
        string APIKey = Environment.GetEnvironmentVariable("APIKey");
        private readonly HttpClient client = new HttpClient();

        public DataFabricManager()
        {
            client.BaseAddress = URL;
            client.DefaultRequestHeaders.Add("x-api-key", APIKey);
        }

        public String dataFabricQuery(String content, string after = "")
        {
            List<String> imagesList = new List<String>();
            // Call asynchronous network methods in a try/catch block to handle exceptions.


            StringContent stringContent = new StringContent(content);
            
            //Synchronous call of client POST
            var task = Task.Run(() => client.PostAsync(URL, stringContent));
            task.Wait();
            HttpResponseMessage responseMessage = task.Result;

            //Synchronous call of response Read
            var resTask =  Task.Run(() => responseMessage.Content.ReadAsStringAsync());
            resTask.Wait();
            string responseBody = resTask.Result;

            List<String> goldenRecordNumberMmrIdList = new List<String>();

            //Deserialize the response body so that we can identify items uniquely.
            Root root = JsonConvert.DeserializeObject<Root>(responseBody);
            List<String> errors = new List<String>();
            try
            {
                //Grab items
                foreach (var items in root.data.getPGRList.items)
                {
                    //Grab mmr in items
                    foreach (var mmr in items.mmr)
                    {
                        //If mmr is something
                        if (mmr.goldenRecordNumberMmrId != null)
                        {
                            //Grab images in items
                            foreach (var image in items.upc.images)
                            {
                                //IF image contains A1N1 suffix 
                                //AND golden id is not in golden id list 
                                //AND image uri not in images list yet
                                if (image.type.Contains("A1N1 - Front")
                                    && !goldenRecordNumberMmrIdList.Contains(mmr.goldenRecordNumberMmrId)
                                    && !imagesList.Contains(image.uniformResourceIdentifier))
                                {
                                    //Add golden id to golden id list
                                    goldenRecordNumberMmrIdList.Add(mmr.goldenRecordNumberMmrId);
                                    //Add image type, uri and golden id to imageslist
                                    imagesList.Add(image.type);
                                    imagesList.Add(image.uniformResourceIdentifier);
                                    imagesList.Add(mmr.goldenRecordNumberMmrId);
                                    Console.WriteLine("\n Image is of type: " + image.type);
                                    Console.WriteLine(" Is the image of type A1N1: " + image.type.Contains("A1N1"));
                                    Console.WriteLine(" Image is of uri: " + image.uniformResourceIdentifier);
                                    Console.WriteLine("Golden record number id is: " + mmr.goldenRecordNumberMmrId + "\n");
                                    //Create a filename as the golden id of the image
                                    string fileName = mmr.goldenRecordNumberMmrId + ".jpg";
                                    //Download uri to selected path with filename
                                    using (var downloadClient = new WebClient())
                                    {
                                        try
                                        {
                                            downloadClient.DownloadFile(new Uri(image.uniformResourceIdentifier),
                                            "/Users/dylancarlyle/Documents/Image Automation/Image-Automation/Downloaded Images/" + fileName);
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
                foreach (var err in root.errors)
                {
                    errors.Add(err.ToString());
                }

            }
            after = root.data.getPGRList.nextToken;
            return after;
        }
        public void dataFabricPaging(String content)
        {
            string token = "";
            while (token != null)
            {
                if (token == "")
                {
                    token = dataFabricQuery(content);
                }
                else
                {
                    string replacement = "after: " + "\\\"" + token + "\\\"" ;
                    string output = "{\"query\":\"{\\n    getPGRList (count:500, " + replacement + "){\\n    items{\\n        upc {\\n        images {\\n            type\\n            uniformResourceIdentifier\\n        }\\n        }\\n        mmr{\\n        goldenRecordNumberMmrId\\n        }\\n    }\\n        nextToken\\n    }\\n} \"}";
                    token = dataFabricQuery(output);
                }
            }
        }
    }
}
