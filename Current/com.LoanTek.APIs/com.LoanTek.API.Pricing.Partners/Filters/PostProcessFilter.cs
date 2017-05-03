using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using com.LoanTek.API.Pricing.Partners.Models;
using LoanTek.LoggingObjects;
using LoanTek.Utilities;

namespace com.LoanTek.API.Pricing.Partners.Filters
{
    /// <summary>
    /// All Responses should use this filter to do some common tasks.
    /// </summary>
    public class PostProcessFilter : ActionFilterAttribute
    {
        //private readonly SimpleLogger.LocationObject locationObject = new SimpleLogger.LocationObject() { ClassName = "PostProcessFilter", MethodName = "ProcessTimeInMilliSecs", Namespace = ""};

        static PostProcessFilter() //A static constructor is used to initialize any static data, or to perform a particular action that needs performed once only
        {
          
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionContext)
        {
            try
            {
                new Task(() =>
                {
                    ApiResponse apiResponse = new ApiResponse();
                    var objContent = (actionContext.ActionContext.Response.Content as ObjectContent);
                    IPartnerResponse content = objContent?.Value as IPartnerResponse;
                    if (content != null)
                    {
                        ClassMappingUtilities.SetPropertiesForTarget(content, apiResponse);
                        apiResponse.Message = content.LoanTekDefinedIdentifier.ToString();
                    }
                    else
                    {
                        apiResponse.Message = (string)((objContent?.Value is string) ? objContent?.Value : actionContext.Response.ReasonPhrase);
                    }
                    apiResponse.HttpStatusCode = actionContext.ActionContext.Response.StatusCode;

                    AApiRequest apiRequest = actionContext.Request.Properties[Global.ARequestPropertyName] as AApiRequest;
                    if (apiRequest != null)
                    {
                        //if the request Id is zero, then the save to the data context may not have completed. Let's give it a little extra time...
                        int counter = 0;
                        while (apiRequest.Id == 0 && counter++ < 20)
                        {
                            Thread.Sleep(50);
                        }
                        apiResponse.Id = apiRequest.Id;
                        apiResponse.ClientDefinedIdentifier = apiRequest.ClientDefinedIdentifier;
                    }    
                    apiResponse.Save();
                }).Start();
            }
            catch (Exception ex)
            {
                Global.OutPrint("ERROR: " + ex.Message, new SimpleLogger.LocationObject(this, "OnActionExecuted"));
            }
        }
    }
}
