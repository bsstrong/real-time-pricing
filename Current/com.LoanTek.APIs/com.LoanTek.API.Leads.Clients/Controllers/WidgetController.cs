using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using com.LoanTek.API.Leads.Clients.Models.Common;
using com.LoanTek.Biz.Api.Objects;
using LoanTek.LoggingObjects;
using WebApiThrottle;

namespace com.LoanTek.API.Leads.Clients.Controllers
{
    /// <summary>
    /// Client web service for use with the LoanTek CRM/Leads/Contact Widgets.
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [RoutePrefix("LeadsWidget/{authToken}")]
    public class WidgetController : AApiController
    {
        public const string WebServiceName = "LeadsWidget";
        public static readonly ApiWebService ApiWebService;
        private const long ratePerSec = 10;
        private const long ratePerMin = 600;

        static WidgetController()
        {
            ApiWebService = Global.LeadWebServices.FirstOrDefault(x => x.WebServiceName == WebServiceName);
        }

        /// <summary>       
        /// Add a new 'Sales' lead to the LoanTek CRM from the LoanTek Contact Widget.  
        /// </summary>
        /// <remarks>
        /// LoanTek Clients are required to have an active CRM license to view and use the CRM. 
        /// </remarks>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <param name="request" cref="SalesLeadRequest"></param>
        /// <returns cref="SalesLeadResponse">On success a SalesLeadResponse object. Else, a string error message.</returns>
        /// <response code="200">SalesLeadResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid Client or User Id)</response>
        [EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin)] //enables Rate Limits
        [Route("Contact")]
        [HttpPost]
        [HttpPut]
        [ResponseType(typeof(SalesLeadResponse))]
        public HttpResponseMessage Add(string authToken, SalesLeadRequest request)
        {
            this.Sw.Start();
            this.EndPoint = "Contact";

            try
            {
                if (ApiWebService.ApiStatusType == Types.Api.ApiStatusType.Inactive)
                    return this.CreateErrorResponse(HttpStatusCode.BadRequest, "In-active Web Service / Endpoint", this.EndPoint);

                HttpResponseMessage errorResponse = this.CommonChecks(request, this.EndPoint);
                if (errorResponse != null)
                    return errorResponse;

                this.AuthToken = !string.IsNullOrEmpty(authToken) ? new AuthToken(authToken) : null;
                errorResponse = this.Authorize(ApiWebService.WebServiceName, new AuthToken(authToken), request.LeadFile?.UserId ?? 0);
                if (errorResponse != null)
                    return errorResponse;

                SalesLeadResponse response = this.Process(request) as SalesLeadResponse;
                if (response == null)
                    throw new InvalidCastException("Error ALeadsResponse should cast to SalesLeadResponse.");

                Task.Run(() => this.CreateAndSaveService(request, response, response.Message, Types.Api.AppType.Widget, ApiWebService)).ConfigureAwait(false);

                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception ex)
            {
                Global.OutPrint(ex.Message, new SimpleLogger.LocationObject(this, this.EndPoint), SimpleLogger.LogLevelType.CRITICAL);
                return this.CreateErrorResponse(HttpStatusCode.InternalServerError, "Exception:" + ex.Message, this.EndPoint);
            }

        }
    }
}
