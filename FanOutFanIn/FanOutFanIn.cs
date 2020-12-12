using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;

using Microsoft.Extensions.Logging;

namespace MvpConf
{
    public static class FanOutFanIn
    {
        [FunctionName("FanOutFanIn")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var tasks = new List<Task<string>>();

            var sales = await context.CallActivityAsync<List<int>>("FetchPaidSales", null);

            foreach (var sale in sales)
            {
                tasks.Add(context.CallActivityAsync<string>("ProcessShippingInformation", sale));
            }

            await Task.WhenAll(tasks);

            return tasks.Select(t => t.Result).ToList();
        }

        [FunctionName("FetchPaidSales")]
        public static List<int> FetchPaidSales([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation("Fetching not sent paid sales");
            // Your DB Code here
            return new List<int>() { 10, 11, 15, 17, 12, 16, 20 };
        }

        [FunctionName("ProcessShippingInformation")]
        public static string ProcessShippingInformation([ActivityTrigger] int idSale, ILogger log)
        {
            log.LogInformation($"Processing shipping information for sale {idSale}");

            // Insert here your logic 

            return $"Processed shipping information for sale {idSale}";
        }

        [FunctionName("FanOutFanIn_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("FanOutFanIn", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}