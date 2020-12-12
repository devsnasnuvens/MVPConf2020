using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace MvpConf
{
    public static class FunctionChaining
    {
        [FunctionName("FunctionChaining")]
        public static async Task<object> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            int invoiceId = context.GetInput<int>();

            try
            {
                var paidInvoice = await context.CallActivityAsync<object>("ProcessPayment", invoiceId);
                var taxReceipt = await context.CallActivityAsync<object>("GenerateTaxReceipt", paidInvoice);
                return await context.CallActivityAsync<object>("SendTaxReceipt", taxReceipt);
            }
            catch (System.Exception)
            {
                // Tratamento de exceções
                return null;
            }
        }

        [FunctionName("ProcessPayment")]
        public static string ProcessPayment([ActivityTrigger] int invoiceId, ILogger log)
        {
            log.LogInformation($"Processing payment for invoice #{invoiceId}.");
            // Add logic payment
            return $"Payment processed for invoice #{invoiceId}!";
        }

        [FunctionName("GenerateTaxReceipt")]
        public static string GenerateTaxReceipt([ActivityTrigger] string paidInvoice, ILogger log)
        {
            log.LogInformation($"Generating Tax Receipt -> {paidInvoice}");
            // call nf
            return $"Tax Receipt generated! -> {paidInvoice}";
        }

        [FunctionName("SendTaxReceipt")]
        public static string SendTaxReceipt([ActivityTrigger] string taxReceipt, ILogger log)
        {
            log.LogInformation($"Sending Tax Receipt -> {taxReceipt}");
            // send nf to client
            return $"Tax Receipt sent! -> {taxReceipt}";
        }

        [FunctionName("FunctionChaining_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "chaining/{invoiceId}")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            int invoiceId,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("FunctionChaining", null, invoiceId);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}