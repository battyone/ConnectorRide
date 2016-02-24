using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Knapcode.ConnectorRide.Web;
using Microsoft.Owin;
using Newtonsoft.Json;
using Owin;

[assembly: OwinStartup(typeof (Startup))]

namespace Knapcode.ConnectorRide.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var httpConfiguration = new HttpConfiguration();

            Register(httpConfiguration);

            app.UseWebApi(httpConfiguration);
        }

        private static void Register(HttpConfiguration config)
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