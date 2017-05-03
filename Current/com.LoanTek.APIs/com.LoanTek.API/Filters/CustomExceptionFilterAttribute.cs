using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace com.LoanTek.API.Filters
{
    public class CustomExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            var error = "<h4>Exception @ "+ context.Request.RequestUri.AbsoluteUri +"</h4> An error occured: " + context.Exception.Message;
            error += "<h4>Please contact support@loantek.com if you continue to see this exception.</h4>";
            context.Response = context.Request.CreateResponse(HttpStatusCode.InternalServerError, error, "text/html");
        }
    }
}