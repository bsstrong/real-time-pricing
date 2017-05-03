using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Cors;
using com.LoanTek.API.Pricing.Partners.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WebApiThrottle;

namespace com.LoanTek.API.Pricing.Partners
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Cross-Origin http://www.asp.net/web-api/overview/security/enabling-cross-origin-requests-in-web-api#enable-cors
            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);

            // Web API routes
            config.MapHttpAttributeRoutes();

            //config.Routes.MapHttpRoute(
            //    name: "Home",
            //    routeTemplate: "Home/{action}/{query}",
            //    defaults: new { action = "Index", query = RouteParameter.Optional }
            //);

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{action}/{query}",
                defaults: new { query = RouteParameter.Optional }
            );

            #region Filters

            //config.Filters.Add(new LoanTekExceptionFilterAttribute()); //DO NOT USE, DOCS WILL TRY TO BE CREATED FOR ALL  Master.WebServices...!
            config.Filters.Add(new ServerStatusFilterAttribute());

            //config.MessageHandlers.Add(new RateLimitHandler()
            config.Filters.Add(new CustomThrottlingFilter()
            {
                Policy = new ThrottlePolicy(perSecond: 60, perMinute: 3600)
                {
                    IpThrottling = false,
                    ClientThrottling = true
                },
                Repository = new CacheRepository()
            });

            #endregion


            //Json Config
            config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Include;
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter()); //convert enums from 'int' to 'string'
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.None;
            config.Formatters.JsonFormatter.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Include;

            //process plain/text or html requests as Json
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));

            //remove xml and Form so it defaults to Json
            config.Formatters.Remove(config.Formatters.FormUrlEncodedFormatter);
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            //config.Formatters.XmlFormatter.UseXmlSerializer = true;
        }

        //https://github.com/stefanprodan/WebApiThrottle
        /// <summary>
        /// Custom ThrottlingFilter that uses the full request path as the 'ClientKey'.
        /// </summary>
        public class CustomThrottlingFilter : ThrottlingFilter
        {
            protected override RequestIdentity SetIdentity(HttpRequestMessage request)
            {
                var path = request.RequestUri.AbsolutePath.ToLowerInvariant();
                return new RequestIdentity()
                {
                    Endpoint = path,
                    ClientKey = path,
                    ClientIp = GetClientIp(request).ToString(),
                };
            }
        }
    }
}
