using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Linq;

namespace CoffeeNChill.Functions.Functions
{
    /// <summary>
    /// Uploads a staff document to the staff-docs blob container.
    /// </summary>
    public class UploadStaffDocument
    {
        private readonly BlobContainerClient _container;

        public UploadStaffDocument()
        {
            var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? string.Empty;
            var client = new BlobServiceClient(conn);
            _container = client.GetBlobContainerClient("staff-docs");
            _container.CreateIfNotExists();
        }

        [Function("UploadStaffDocument")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents/upload")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("UploadStaffDocument");
            try
            {
                if (!req.Headers.TryGetValues("Content-Type", out var contentTypes))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Content-Type header missing");
                    return bad;
                }

                var contentType = contentTypes.FirstOrDefault();
                if (string.IsNullOrEmpty(contentType) || !contentType.Contains("multipart/form-data"))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Content-Type must be multipart/form-data");
                    return bad;
                }

                var mediaType = MediaTypeHeaderValue.Parse(contentType);
                var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
                var reader = new MultipartReader(boundary, req.Body);
                var section = await reader.ReadNextSectionAsync();
                bool uploaded = false;
                while (section != null)
                {
                    var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                    if (hasContentDispositionHeader && contentDisposition != null && contentDisposition.DispositionType.Equals("form-data") && !string.IsNullOrEmpty(contentDisposition.FileName.Value))
                    {
                        var fileName = contentDisposition.FileName.Value.Trim('"');
                        var blob = _container.GetBlobClient(fileName);
                        await blob.UploadAsync(section.Body, overwrite: true);
                        uploaded = true;
                        break;
                    }

                    section = await reader.ReadNextSectionAsync();
                }

                if (!uploaded)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("No file provided in multipart body");
                    return bad;
                }

                var resp = req.CreateResponse(HttpStatusCode.Created);
                await resp.WriteStringAsync("Uploaded");
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading staff document");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteStringAsync("Internal Server Error");
                return resp;
            }
        }
    }
}
