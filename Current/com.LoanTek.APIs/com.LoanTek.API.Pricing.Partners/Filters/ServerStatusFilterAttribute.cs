using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace com.LoanTek.API.Pricing.Partners.Filters
{
    public class ServerStatusFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (Global.ServerStatusType == Types.Api.ApiStatusType.Inactive || Global.ServerStatusType == Types.Api.ApiStatusType.Down)
            {
                actionContext.Response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(Global.ServerStatusType.ToString())
                };
            }
            base.OnActionExecuting(actionContext);
        }
    }  
}