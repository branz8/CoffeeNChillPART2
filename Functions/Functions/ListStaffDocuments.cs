using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions
{
    /// <summary>
    /// Lists blobs in the staff-docs container.
    /// </summary>
    public class ListStaffDocuments
    {
        private readonly BlobContainerClient _container;

        public ListStaffDocuments()
        {
            var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? string.Empty;
            var client = new BlobServiceClient(conn);
            _container = client.GetBlobContainerClient("staff-docs");
            _container.CreateIfNotExists();
        }

        [Function("ListStaffDocuments")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("ListStaffDocuments");
            try
            {
                var items = _container.GetBlobsAsync();
                var list = new List<object>();
                await foreach (var b in items)
                {
                    list.Add(new
                    {
                        Name = b.Name,
                        Size = b.Properties.ContentLength ?? 0,
                        LastModified = b.Properties.LastModified,
                        ContentType = b.Properties.ContentType
                    });
                }

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteAsJsonAsync(list);
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error listing staff documents");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteStringAsync("Internal Server Error");
                return resp;
            }
        }
    }
}
