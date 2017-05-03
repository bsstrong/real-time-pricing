using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Cors;
using Newtonsoft.Json;

namespace com.LoanTek.API.Common
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Cross-Origin http://www.asp.net/web-api/overview/security/enabling-cross-origin-requests-in-web-api#enable-cors
            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);

            //config.Filters.Add(new com.LoanTek.Master.WebService.Filters.LoanTekExceptionFilterAttribute());

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{action}/{query}",
                defaults: new { query = RouteParameter.Optional }
            );

            //config.MessageHandlers.Add(new RateLimitHandler()
            /*
            config.Filters.Add(new ThrottlingFilter()
            {
                Policy = new ThrottlePolicy(perSecond: 10, perMinute: 60)
                {
                    IpThrottling = false,
                    ClientThrottling = true
                    //,ClientWhitelist = new List<string> { "1020W.MainSt." }
                },
                Repository = new CacheRepository()
            });
            */

            //Json Config
            config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            //config.Formatters.JsonFormatter.SerializerSettings.MaxDepth = 10;

            //process plain/text or html requests as Json
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));

            //remove xml and Form so it defaults to Json
            config.Formatters.Remove(config.Formatters.FormUrlEncodedFormatter);
            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}
