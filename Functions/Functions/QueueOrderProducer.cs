using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using CoffeeNChill.Functions;

namespace CoffeeNChill.Functions.Functions
{
    /// <summary>
    /// Accepts an Order JSON and enqueues it to order-processing-queue.
    /// </summary>
    public class QueueOrderProducer
    {
        private readonly QueueClient _queueClient;

        public QueueOrderProducer(QueueClient queueClient)
        {
            _queueClient = queueClient;
            _queueClient.CreateIfNotExists();
        }

        [Function("QueueOrderProducer")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/queue")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("QueueOrderProducer");
            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var order = JsonSerializer.Deserialize<Order>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (order == null || string.IsNullOrEmpty(order.OrderId))
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteStringAsync("Invalid order payload");
                    return bad;
                }

                var json = JsonSerializer.Serialize(order);
                var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
                await _queueClient.SendMessageAsync(base64);
                logger.LogInformation("Enqueued order {OrderId} as base64", order.OrderId);

                var resp = req.CreateResponse(HttpStatusCode.OK);
                await resp.WriteStringAsync("Enqueued");
                return resp;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error queuing order");
                var resp = req.CreateResponse(HttpStatusCode.InternalServerError);
                await resp.WriteStringAsync("Internal Server Error");
                return resp;
            }
        }
    }
}
