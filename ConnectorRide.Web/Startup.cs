using System;
using System.Web.Http;
using System.Web.Http.Routing;
using Knapcode.ConnectorRide.Web;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof (Startup))]

namespace Knapcode.ConnectorRide.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var httpConfiguration = new HttpConfiguration();

            WebApiConfig.Register(httpConfiguration);

            app.UseWebApi(httpConfiguration);
        }
    }
}