using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Results;
using Knapcode.ConnectorRide.Core;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Newtonsoft.Json;

namespace Knapcode.ConnectorRide.Web
{
    public class ExceptionHandler : IExceptionHandler
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        };

        public Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
        {
            var content = JsonConvert.SerializeObject(context.Exception, JsonSerializerSettings);
            var response = new HttpResponseMessage
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
                StatusCode = HttpStatusCode.InternalServerError
            };

            if (context.Exception is ThrottlingException)
            {
                response.StatusCode = (HttpStatusCode) 429;
                response.ReasonPhrase = "Too Many Requests";
            }

            context.Result = new ResponseMessageResult(response);
            return Task.FromResult((object) null);
        }
    }
}