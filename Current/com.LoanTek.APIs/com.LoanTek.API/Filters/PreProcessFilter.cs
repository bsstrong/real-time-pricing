using System;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using LoanTek.Utilities;

namespace com.LoanTek.API.Filters
{
    public class PreProcessFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            //new Task(() => { ).Start(); //this will execute in its own thread allowing the request to continue processing independently of the 'Save'
            //var durationInMilli = (DateTime.Now - startTime).Milliseconds;
            //Debug.WriteLine("PreProcessQuoteRequestFilter hit...pre-process time: " + durationInMilli);
            
            actionContext.Request.Properties["StartTime"] = DateTime.Now;
            actionContext.Request.Properties["UniqueId"] = StringUtilities.UniqueId();

            base.OnActionExecuting(actionContext);
        }
    }
}
