using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace com.LoanTek.API.Demo
{
    public class Global : System.Web.HttpApplication
    {
        public static JsonSerializerSettings JsonSettings;

        protected void Application_Start()
        {
            JsonSettings = new JsonSerializerSettings
            {   
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                DefaultValueHandling = DefaultValueHandling.Populate,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            JsonSettings.Converters.Add(new StringEnumConverter()); //convert enums from 'int' to 'string'

            HttpConfiguration config = GlobalConfiguration.Configuration;
            config.Formatters.JsonFormatter.SerializerSettings = JsonSettings;

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
