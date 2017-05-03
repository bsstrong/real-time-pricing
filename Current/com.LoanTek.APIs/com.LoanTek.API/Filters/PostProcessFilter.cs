using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using com.LoanTek.API.Requests;
using LoanTek.LoggingObjects;

namespace com.LoanTek.API.Filters
{
    /// <summary>
    /// All Quote Requests should use this filter to do some common tasks.
    /// </summary>
    public class PostProcessFilter : ActionFilterAttribute
    {
        private static readonly Logger requestLogger = new Logger(SimpleLogger.LogToType.FILE);
        private readonly SimpleLogger.LocationObject locationObject = new SimpleLogger.LocationObject() { ClassName = "PostProcessFilter", MethodName = "ProcessTimeInMilliSecs", Namespace = ""};

        static PostProcessFilter() //A static constructor is used to initialize any static data, or to perform a particular action that needs performed once only
        {
            if (string.IsNullOrEmpty(requestLogger.FileDefault))
                requestLogger.CreateLogFile("PostProcessFilter.txt");
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionContext)
        {
            string uniqueId = null;
            try
            {
                uniqueId = actionContext.Request.Properties["UniqueId"] as string;

                var objContent = (actionContext.ActionContext.Response.Content as ObjectContent);
                IApiResponse apiResponse = objContent?.Value as IApiResponse;

                DateTime startTime = (DateTime)actionContext.Request.Properties["StartTime"];
                int totalTimeDurationInMillisec = (int) (DateTime.Now - startTime).TotalMilliseconds;
                if (apiResponse != null)
                {
                    //response.MilliSecSinceRequestStart = totalTimeDurationInMillisec;
                    objContent.Value = apiResponse;
                }
                else
                {
                    throw new Exception("IResponse object not found!");
                }

                new Task(() =>
                {
                    requestLogger.Log(SimpleLogger.LogLevelType.INFO, uniqueId + requestLogger.FileDelimiterDefault + totalTimeDurationInMillisec + requestLogger.FileDelimiterDefault, locationObject);
                }).Start(); 
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR @ PostProcessFilter.OnActionExecuted: " + ex.Message);
                requestLogger.Log(SimpleLogger.LogLevelType.ERROR, uniqueId + requestLogger.FileDelimiterDefault + ex.Message, locationObject);
            }
        }
    }
}
