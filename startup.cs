
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

[assembly: FunctionsStartup(typeof(Image.Startup))]

namespace Image
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging();
            builder.Services.AddSingleton<DataFabricManager>();
            builder.Services.AddHttpClient();
            builder.Services.AddDurableClientFactory();
        }
    }
}
