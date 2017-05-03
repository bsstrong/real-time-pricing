using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using com.LoanTek.API.Common.Models;
using com.LoanTek.API.Pricing.Partners.Filters;
using com.LoanTek.API.Pricing.Partners.Models.Common;
using com.LoanTek.API.Requests;
using com.LoanTek.Forms.Mortgage;
using com.LoanTek.Quoting.Mortgage.Common;
using com.LoanTek.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace com.LoanTek.API.Pricing.Partners.Models
{
    /// <summary>
    /// Wrapper class for ApiController providing some common properties and methods
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class AApiController : ApiController
    {
        protected CommonParams CommonParams;
        protected string EndPoint;
        protected bool DoNotInsert;

        protected CancellationTokenSource StartTimeoutTimer(int timeoutInMill)
        {
            CancellationTokenSource cancelToken = new CancellationTokenSource();
            Task.Run(async () =>
            {
                await Task.Delay(timeoutInMill);
                cancelToken.Cancel();
            });
            return cancelToken;
        }

        protected HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string message, string apiEndPoint)
        {
            ApiResponse apiResponse = new ApiResponse();
            apiResponse.ApiEndPoint = apiEndPoint;
            apiResponse.Message = message;
            return Request.CreateResponse(statusCode, apiResponse, new JsonMediaTypeFormatter() { SerializerSettings = Global.JsonSettings });
        }

        /// <summary>
        /// Internal method that performs checks that should be common to most controller requests
        /// </summary>
        /// <param name="request"></param>
        /// <param name="apiEndPoint"></param>
        /// <returns></returns>
        protected HttpResponseMessage CommonChecks(FullMortgageRequest request, string apiEndPoint)
        {
            if (request == null)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Request missing required Mortgage Request.", apiEndPoint);

            if (string.IsNullOrEmpty(request.ClientDefinedIdentifier))
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Request missing required ClientDefinedIdentifier.", apiEndPoint);

            if (request.LoanRequest == null)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Request missing required Mortgage Loan Request.", apiEndPoint);

            if (string.IsNullOrEmpty(request.LoanRequest?.ZipCode) || !request.LoanRequest.IsValid)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "FullMortgageRequest missing required fields"+ ((request.LoanRequest.ValidationErrors.Count == 0) ? "." : ": "+ string.Join(";", request.LoanRequest.ValidationErrors)), apiEndPoint);

            if (request.LoanRequest?.QuotingChannel == QuotingChannelType.NotSpecified)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing QuotingChannelType in Loan Request", apiEndPoint);

            return null;
        }

        protected HttpResponseMessage CommonChecks(MortgageRequest request, string apiEndPoint)
        {
            if (request == null)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Request missing required Mortgage Request.", apiEndPoint);

            if (string.IsNullOrEmpty(request.ClientDefinedId))
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Request missing required ClientDefinedId.", apiEndPoint);

            if (request.Form == null)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Request missing required Mortgage Loan Request Form.", apiEndPoint);

            if (request.Form?.QuotingChannelType == QuotingChannelType.NotSpecified)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing QuotingChannelType in Loan Request Form", apiEndPoint);

            return null;
        }

        /// <summary>
        /// This method can be overriden in a controller class to provide more specific Authorize for certain actions 
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="authTokenObject"></param>
        /// <param name="partnerId"></param>
        /// <returns></returns>
        protected virtual HttpResponseMessage Authorize(string endPoint, AuthToken authTokenObject, int partnerId)
        {
            if (authTokenObject == null)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid or missing required Auth Token", endPoint);

            if (string.IsNullOrEmpty(authTokenObject.ApiName) || authTokenObject.ClientId < 1)
                return this.CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid Authentication Token", endPoint);
            if (!endPoint.StartsWith(authTokenObject.ApiName))
                return this.CreateErrorResponse(HttpStatusCode.Unauthorized, "Unauthorized Authentication Token", endPoint);

            if (authTokenObject.ClientId != partnerId)
                return this.CreateErrorResponse(HttpStatusCode.Forbidden, "Partner is invalid", endPoint);

            return null;
        }

        /// <summary>
        /// Internal method that performs processes that should be common to most controller requests
        /// </summary>
        /// <param name="request">HttpRequestMessage</param>
        protected CommonParams CommonProcesses(HttpRequestMessage request)
        {
            //can also inherit this class and override default values...
            if (this.CommonParams == null)
                this.CommonParams = new CommonParams(Request);
            return this.CommonParams;
        }

        protected List<string> PropertiesToSerialize;
        [ApiExplorerSettings(IgnoreApi = true)]
        public virtual List<string> GetPropertiesToSerialize(string customQuoteJson)
        {
            if (string.IsNullOrEmpty(customQuoteJson))
                this.PropertiesToSerialize = null;
            return this.PropertiesToSerialize ?? (this.PropertiesToSerialize = JObject.Parse(customQuoteJson ?? JsonConvert.SerializeObject(new MortgageLoanQuote(), new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Include, NullValueHandling = NullValueHandling.Include, ReferenceLoopHandling = ReferenceLoopHandling.Ignore }))?.Children().Select(child => child.Path).ToList());
        }

        

    }
}