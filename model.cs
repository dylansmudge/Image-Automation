using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Image
{
 //Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Data
    {
        public GetPGRList getPGRList { get; set; }
    }

    public class GetPGRList
    {
        public List<Items> items { get; set; }
        public string nextToken { get; set; }
    }

    public class Images
    {
        public string type { get; set; }
        public string uniformResourceIdentifier { get; set; }
    }

    public class Items
    {
        public Upc upc { get; set; }
        public List<Mmr> mmr { get; set; }
    }

    public class Mmr
    {
        public string goldenRecordNumberMmrId { get; set; }
    }

    public class Root
    {
        public Data data { get; set; }
    }

    public class Upc
    {
        public List<Images> images { get; set; }
    }



}