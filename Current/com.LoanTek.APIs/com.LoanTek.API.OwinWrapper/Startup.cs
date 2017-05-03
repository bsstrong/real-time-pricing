using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using com.LoanTek.API.Pricing.Partners.Controllers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Owin;

namespace com.LoanTek.API.OwinWrapper
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            int maxWorkerThreads;
            int maxCompletionThreads;
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionThreads);
            Debug.WriteLine("-maxWorkerThreads:" + maxWorkerThreads);
            Debug.WriteLine("-maxCompletionThreads:" + maxCompletionThreads);


            var config = new HttpConfiguration();
            config.Services.Replace(typeof(IAssembliesResolver), new AssembliesResolver());
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                "Default",
                "{controller}/{action}/{query}",
                new {action = "Index", query = RouteParameter.Optional});
            
            #region Formatters
            ////Json Config
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
            #endregion

            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

            app.UseWebApi(config);

            config.EnsureInitialized();

            if (Global.Config.AssemblyName?.Contains("Pricing") ?? false)
            {
                if (Global.Config.AssemblyName.Contains("Partners"))
                {
                    Pricing.Partners.Global.InitData();
                    updateCacheValues();
                }
                //if (Global.Config.AssemblyName.Contains("Clients"))
                //    Pricing.Clients.Global.InitData();
            }
            else if(Global.Config.AssemblyName?.Contains("Client") ?? false)
            {
                throw new NotImplementedException("ONLY Pricing APIs have been implemented!");
            }
        }

        private void updateCacheValues()
        {
            if (!string.IsNullOrEmpty(Global.Config.CacheHost))
            {
                BankrateController.CacheHost = Global.Config.CacheHost;
                BankrateController.CachePort = Global.Config.CachePort;
                //TODO - Update other Partner Controllers here...
                //CommonController.CacheHost = Global.Config.CacheHost;
                //CommonController.CachePort = Global.Config.CachePort;
            }
        }

        
    }

    public class AssembliesResolver : DefaultAssembliesResolver
    {
        public override ICollection<Assembly> GetAssemblies()
        {
            List<Assembly> assemblies = new List<Assembly>();

            string binPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string path = binPath + "\\com.LoanTek.API.OwinWrapper.exe";
            if (File.Exists(path))
            {
                var controllersAssembly = Assembly.LoadFrom(path);
                assemblies.Add(controllersAssembly);
            }
            else
                throw new Exception("Missing app's default assembly. File not found:"+ path);
            
            path = binPath +"\\"+ Global.Config.AssemblyName;
            if (File.Exists(path))
            {
                //ICollection<Assembly> baseAssemblies = base.GetAssemblies();
                //baseAssemblies.Add(controllersAssembly);

                var controllersAssembly = Assembly.LoadFrom(path);           
                assemblies.Add(controllersAssembly);
            }
            return assemblies;
        }
    }
}
