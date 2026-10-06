using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using CoffeeNChill.Functions;

namespace CoffeeNChill.Functions.Functions
{
    /// <summary>
    /// Creates a new menu item in the MenuItems table.
    /// </summary>
    public class CreateMenuItem
    {
        private readonly TableStorageService _tableService;

        public CreateMenuItem(TableStorageService tableService)
        {
            _tableService = tableService;
        }

        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("CreateMenuItem");
            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var item = JsonSerializer.Deserialize<MenuItem>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (item == null || string.IsNullOrEmpty(item.Category) || string.IsNullOrEmpty(item.Id))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid menu item payload. Category and Id are required.");
                    return bad;
                }

                await _tableService.InsertMenuItemAsync(item);

                var resp = req.CreateResponse(HttpStatusCode.Created);
                await resp.WriteStringAsync("Created");
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating menu item");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteStringAsync("Internal Server Error");
                return resp;
            }
        }
    }
}
