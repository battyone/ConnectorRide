using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Newtonsoft.Json;

namespace Knapcode.ConnectorRide.Web
{
    public class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter
            {
                UseDataContractJsonSerializer = false,
                SerializerSettings = new JsonSerializerSettings()
            });

            config.Services.Replace(typeof(IExceptionHandler), new ExceptionHandler());

            config.Routes.MapHttpRoute(
                "CommandsUpdate",
                "commands/update",
                new { controller = "Commands", action = "UpdateSchedulesAsync" });
        }
    }
}