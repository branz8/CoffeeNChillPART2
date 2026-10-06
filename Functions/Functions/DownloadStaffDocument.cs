using System;
using System.Net;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions
{
    /// <summary>
    /// Downloads a staff document from blob storage and streams it back to the client.
    /// </summary>
    public class DownloadStaffDocument
    {
        private readonly BlobContainerClient _container;

        public DownloadStaffDocument()
        {
            var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? string.Empty;
            var client = new BlobServiceClient(conn);
            _container = client.GetBlobContainerClient("staff-docs");
            _container.CreateIfNotExists();
        }

        [Function("DownloadStaffDocument")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents/download/{fileName}")] HttpRequestData req,
            string fileName,
            FunctionContext context)
        {
            var logger = context.GetLogger("DownloadStaffDocument");
            try
            {
                var blob = _container.GetBlobClient(fileName);
                if (!await blob.ExistsAsync())
                {
                    var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFound.WriteStringAsync("File not found");
                    return notFound;
                }

                var download = await blob.DownloadAsync();
                var resp = req.CreateResponse(HttpStatusCode.OK);
                if (!string.IsNullOrEmpty(download.Value.ContentType))
                {
                    resp.Headers.Add("Content-Type", download.Value.ContentType);
                }

                await download.Value.Content.CopyToAsync(resp.Body);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error downloading staff document");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteStringAsync("Internal Server Error");
                return resp;
            }
        }
    }
}
