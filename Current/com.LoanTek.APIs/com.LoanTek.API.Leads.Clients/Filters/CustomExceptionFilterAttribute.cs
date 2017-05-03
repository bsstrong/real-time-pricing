using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http.Filters;
using com.LoanTek.API.Leads.Clients.Models;
using LoanTek.LoggingObjects;
using LoanTek.Utilities;
using Newtonsoft.Json;

namespace com.LoanTek.API.Leads.Clients.Filters
{
    public class CustomExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            Global.OutPrint("Uncaught error: " + context.Exception.Message, "com.LoanTek.API.Pricing.Clients", context.Request.RequestUri.AbsoluteUri, String.Empty, SimpleLogger.LogLevelType.ERROR);

            AApiResponse apiResponse = new AApiResponse();
            apiResponse.ApiEndPoint = context.Request.RequestUri.AbsoluteUri;
            apiResponse.Message = "An error occured: " + context.Exception.Message +". ";
            apiResponse.Message += "Please contact support@loantek.com if you continue to see this exception.";
            string ip = ClientInfo.GetIPAddress(context.Request);
            if (ip.StartsWith("::1") || ip.StartsWith("10.0"))
            {
                string s = JsonConvert.SerializeObject(apiResponse, Global.JsonSettings);
                s += "<hr /><h5>Exception:</h5><ul><li>Message:" + context.Exception.Message + "</li>";
                s += "<li>Source:"+ context.Exception.Source +"</li>";
                s += "<li>StackTrace:" + context.Exception.StackTrace + "</li></ul>";
                context.Response = context.Request.CreateResponse(HttpStatusCode.InternalServerError, s);
            }
            else
                context.Response = context.Request.CreateResponse(HttpStatusCode.InternalServerError, apiResponse, new JsonMediaTypeFormatter() { SerializerSettings = Global.JsonSettings });
            
        }

        
    }
}