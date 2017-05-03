using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using com.LoanTek.API.Pricing.Partners.Controllers;
using LoanTek.Pricing.LoanRequests;
using LoanTek.Utilities;
using Newtonsoft.Json;

namespace com.LoanTek.API.Pricing.Partners.Filters
{
    /// <summary>
    /// All Requests should use this filter to do some common tasks.
    /// </summary>
    public class PreProcessFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            //use with the attribute [SkipMyGlobalActionFilter]
            if (actionContext.ActionDescriptor.GetCustomAttributes<SkipMyGlobalActionFilterAttribute>(false).Any()) { return; }

            //new Task(() => { ).Start(); //this will execute in its own thread allowing the request to continue processing independently of the 'Save'
            //var durationInMilli = (DateTime.Now - startTime).Milliseconds;
            //Debug.WriteLine("PreProcessQuoteRequestFilter hit...pre-process time: " + durationInMilli);

            var newRequest = new AApiRequest();
            actionContext.Request.Properties[Global.ARequestPropertyName] = newRequest;
            Task.Run(() =>
            {
                newRequest.LocalServerName = Global.LocalServerName;
                newRequest.ApiName = Global.ApiObject.ApiName;
                newRequest.ApiEndPoint = actionContext.Request.RequestUri.AbsolutePath.ToLower();
                newRequest.Url = actionContext.Request.RequestUri.AbsoluteUri;
                if (newRequest.ApiEndPoint.Contains("zillow"))
                    newRequest.PartnerId = ZillowController.Partner.ApiPartnerId;
                newRequest.RemoteIP = ClientInfo.GetIPAddress(actionContext.Request);
                try
                {
                    foreach (var key in actionContext.ActionArguments.Keys)
                    {
                        switch (key.ToLower())
                        {
                            case "authtoken": newRequest.AuthToken = actionContext.ActionArguments[key].ToString(); break;
                            case "partnerid": newRequest.PartnerId = NullSafe.NullSafeInteger(actionContext.ActionArguments[key]); break;
                            case "request": newRequest.RawRequest = actionContext.ActionArguments[key].ToString(); break;
                            case "loanpricerloanrequest": newRequest.RawRequest = JsonConvert.SerializeObject(actionContext.ActionArguments[key]); break;
                            case "commonparams": break;
                            default: newRequest.RawRequest = JsonConvert.SerializeObject(actionContext.ActionArguments.FirstOrDefault()); break;
                        }
                    }
                }
                catch (Exception ex) { newRequest.RawRequest = "ERROR trying to read Content:" + ex.Message; }
                if (string.IsNullOrEmpty(newRequest.RawRequest))
                    newRequest.RawRequest = actionContext.Request.RequestUri.Query;
                newRequest.Save();
            });

            base.OnActionExecuting(actionContext);
        }
    }

    public class SkipMyGlobalActionFilterAttribute : Attribute
    {
    }
}
