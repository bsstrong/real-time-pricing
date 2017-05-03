using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.UI.WebControls;

namespace com.LoanTek.API.Leads.Facebook
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = System.Web.Http.RouteParameter.Optional }
            );
        }
    }
}
