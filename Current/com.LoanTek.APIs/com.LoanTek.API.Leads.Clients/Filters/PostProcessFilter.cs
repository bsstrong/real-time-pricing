using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using com.LoanTek.API.Leads.Clients.Models;
using com.LoanTek.API.Requests;
using LoanTek.LoggingObjects;

namespace com.LoanTek.API.Leads.Clients.Filters
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
                    AApiResponse apiResponse = new AApiResponse();
                    AApiRequest request = actionContext.Request.Properties[Global.ARequestPropertyName] as AApiRequest;
                    if (request != null)
                    {
                        //if the request Id is zero, then the save to the data context may not have completed. Let's give it a little extra time...
                        int counter = 0;
                        while (request.Id == 0 && counter++ < 20)
                        {
                            Thread.Sleep(50);
                        }
                        apiResponse.Id = request.Id;
                        apiResponse.ClientDefinedIdentifier = request.ClientDefinedIdentifier;
                    }
                    var objContent = (actionContext.ActionContext.Response.Content as ObjectContent);
                    Requests.IResponse content = objContent?.Value as Requests.IResponse;
                    if (content != null)
                    {
                        apiResponse.ApiEndPoint = content.ApiEndPoint;
                        apiResponse.Message = content.LoanTekDefinedIdentifier.ToString();
                        apiResponse.ExecutionTimeInMillisec = content.ExecutionTimeInMillisec;
                        apiResponse.HttpStatusCode = actionContext.ActionContext.Response.StatusCode;
                        apiResponse.Save();
                    }
                }).Start(); 
            }
            catch (Exception ex)
            {
                Global.OutPrint("ERROR: " + ex.Message, new SimpleLogger.LocationObject(this, "OnActionExecuted"), SimpleLogger.LogLevelType.ERROR);
            }
        }
    }
}
