using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Http.Description;
using LoanTek.Utilities;

namespace com.LoanTek.API
{
    public class ApiDummy : Api
    {
        public ApiDummy()
        {
            this.ApiName = "FullMortgageRequest";
            this.RoutePrefix = "{authToken}/Pricing/{versionId}/Clients/{userId}";
            this.Route = "FullMortgageRequest";
            this.Formats = new List<FormatType>() {FormatType.JSON, FormatType.XML};
            this.RequestObjectType = new object().GetType();
            this.ResponseObjectType = new object().GetType();
            this.Protocal = "REST/Http";
            this.AuthRequired = true;
            this.RateLimited = true;
            this.RateLimitPerMinute = 60;
            this.Versions = new List<Version>()
            {
                new Version()
                {
                    MajorVersionId = 1, MinorVersionId = 1, VersionStatus = Types.Api.VersionStatusType.Current, Created = DateTime.Now.AddDays(-10), LastUpdated = DateTime.Now.AddDays(-1)
                }
            };
            this.SdKs = new List<Sdk>()
            {
                new Sdk()
                {
                    RootObject = new object().GetType(),
                    Language = Sdk.LangType.CSharp,
                    Version = new Version()
                    {
                        MajorVersionId = 1, MinorVersionId = 0, VersionStatus = Types.Api.VersionStatusType.Current, Created = DateTime.Now.AddDays(-10), LastUpdated = DateTime.Now.AddDays(-1)
                    }
                }
            };
        }
    }

    public class Api
    {
        public Api() { }

        public Api(ApiDescription apiDesc)
        {
            this.Route = apiDesc.Route.RouteTemplate;
            int i = this.Route.LastIndexOf("/");
            this.EndpointName = (i != -1) ? this.Route.Substring(i + 1) : this.Route;
            this.RequestObjectType = apiDesc.ParameterDescriptions.FirstOrDefault(x => x.Name == "request")?.ParameterDescriptor.ParameterType;
            this.ResponseObjectType = apiDesc.ActionDescriptor.GetCustomAttributes<ResponseTypeAttribute>()?.FirstOrDefault()?.ResponseType;

            this.Formats = new List<FormatType>();
            //apiDesc.SupportedResponseFormatters.ForEach(x => this.Formats.Add(EnumLib.GetValueFromDescription<Api.FormatType>(x.SupportedMediaTypes.FirstOrDefault()?.MediaType)));
            foreach (var format in apiDesc.SupportedResponseFormatters)
            {
                if (format.SupportedMediaTypes.Any(x => x.ToString().ToLower().Contains("json")))
                    this.Formats.Add(FormatType.JSON);
                if (format.SupportedMediaTypes.Any(x => x.ToString().ToLower().Contains("xml")))
                    this.Formats.Add(FormatType.XML);
            }

            this.Methods = new List<MethodType>();
            apiDesc.ActionDescriptor.SupportedHttpMethods.ForEach(x => this.Methods.Add(EnumLib.Parse<MethodType>(x.Method)));

            var throttleAttr = apiDesc.ActionDescriptor.GetCustomAttributes<WebApiThrottle.EnableThrottlingAttribute>()?.FirstOrDefault();
            this.RateLimitPerSecond = throttleAttr?.PerSecond ?? -1;
            this.RateLimitPerMinute = throttleAttr?.PerMinute ?? -1;  
        }

        public string WebServiceName { get; set; }
        public string EndpointName { get; set; }
        public string ApiName { get; set; }
        public string RoutePrefix { get; set; }
        public string Route { get; set; }

        public enum FormatType
        {
            [Description("application/json")] JSON = 1,
            [Description("application/xml")] XML = 2,
        }
        public List<FormatType> Formats { get; set; }

        public enum MethodType
        {
            [Description("HEAD")]
            HEAD = 1,
            [Description("GET")]
            GET = 2,
            [Description("POST")]
            POST = 3,
            [Description("PUT")]
            PUT = 4,
            [Description("DELETE")]
            DELETE = 5
        }
        public List<MethodType> Methods { get; set; }

        public Type RequestObjectType { get; set; }
        public Type ResponseObjectType { get; set; }
        public Type ResponsePostbackObjectType { get; set; }
        public Type ResponseQuoteObjectType { get; set; }
        public string Protocal { get; set; }
        public bool? AuthRequired { get; set; }
        public bool? RateLimited { get; set; }
        public long? RateLimitPerSecond { get; set; }
        public long? RateLimitPerMinute { get; set; }

        public List<Version> Versions { get; set; }
        public List<Sdk> SdKs { get; set; }
        public List<Server> Servers { get; set; }

        public List<Server> SetServer(Uri requestUri, string localServerName, Types.Api.ApiStatusType serverStatusType)
        {
            return this.Servers ?? (this.Servers = new List<Server>()
            {
                new Server()
                {
                    ServerName = localServerName,
                    Domain = requestUri.Scheme + Uri.SchemeDelimiter + requestUri.Host + (requestUri.IsDefaultPort ? "" : ":" + requestUri.Port),
                    ServerStatus = serverStatusType
                }
            });
        }
    }

    public class Sdk
    {
        public Type RootObject { get; set; }
        public string Desc { get; set; }
        public Version Version { get; set; }
        public enum LangType
        {
            [Description("C#")]
            CSharp = 1,
            [Description("Java")]
            Java = 2,
            [Description("Javascript/Jquery")]
            Javascript = 3
        }
        public LangType Language { get; set; }
    }

    public class Version
    {
        public string Id => MajorVersionId +"."+ MinorVersionId;
        public int MajorVersionId = 1;
        public int MinorVersionId;

        public Types.Api.VersionStatusType VersionStatus { get; set; }

        public string Name { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime Created { get; set; }
        public string ReleaseNotes { get; set; }
    }

    public class Server
    {
        public string ServerName { get; set; }

        public Types.Api.ApiStatusType ServerStatus { get; set; }

        public string Domain { get; set; }
        public string Port { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime Created { get; set; }
    }
}
