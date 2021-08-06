using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BOMjak.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BOMjak.Function
{
    public static class GetBOMjak
    {
        [Function("GetBOMjak")]
        public static async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);

            response.Headers.Add("Content-Type", "image/png");

            using (var stream = await new BOMJakManager(Core.Model.LocationCode.IDR023).CreateAnimatedAsync())
            {
                stream.CopyTo(response.Body);
            }

            return response;
        }
    }
}
