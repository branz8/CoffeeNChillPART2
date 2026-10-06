using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using CoffeeNChill.Functions;

namespace CoffeeNChill.Functions
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddEnvironmentVariables();
                })
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {
                    var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? string.Empty;
                    // Table storage service
                    services.AddSingleton(new TableStorageService(conn));
                    // Queue client for order-processing-queue
                    services.AddSingleton(sp => new Azure.Storage.Queues.QueueClient(conn, "order-processing-queue"));
                    // Blob service client and staff-docs container
                    services.AddSingleton(sp => {
                        var blobService = new Azure.Storage.Blobs.BlobServiceClient(conn);
                        var container = blobService.GetBlobContainerClient("staff-docs");
                        container.CreateIfNotExists();
                        return container;
                    });
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                })
                .Build();

            host.Run();
        }
    }
}
