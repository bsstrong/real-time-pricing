using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using com.LoanTek.Biz.Api.Objects;
using com.LoanTek.IData;
using com.LoanTek.Types;
using com.LoanTek.Quoting.Mortgage.Common;
using com.LoanTek.Quoting.Mortgage.IData;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WebApiThrottle;
using Cache = com.LoanTek.Quoting.Mortgage.Common.Cache;
using FullMortgageRequest = com.LoanTek.API.Pricing.Partners.Models.Mortgage.FullMortgageRequest;
using FullMortgageResponse = com.LoanTek.API.Pricing.Partners.Models.Mortgage.FullMortgageResponse<com.LoanTek.Quoting.Mortgage.Common.MortgageSubmission<com.LoanTek.Quoting.Mortgage.Common.MortgageQuote>, com.LoanTek.Quoting.Mortgage.Common.MortgageQuote>;


namespace com.LoanTek.API.Pricing.Clients.Controllers
{
    /// <summary>
    /// Client Web service (and Widgets) for Quoting Mortgage Rate requests.
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [RoutePrefix("MortgageLoan/Widget/{authToken}")]
    public class MortgageWidgetController : MortgageLoanPricerController
    {
        private const int loanTekPartnerId = 1;
        private const string webServiceName = "MortgageLoanWidget";

        public override long RatePerSec => ratePerSec;
        public override long RatePerMin => ratePerMin;
        public override ApiWebService ApiWebService { get { return apiWebService; } set { } }
        public override JsonSerializerSettings JsonSerializerSettings { get { return jsonSerializerSettings; } set { } }

        private const long ratePerSec = 20;
        private const long ratePerMin = 1200;
        private static readonly ApiWebService apiWebService;
        private static JsonSerializerSettings jsonSerializerSettings;

        private static readonly object syncInit = new object();
        //Static constructor is used to initialize any static data or to perform a particular action that needs to be preformed once only
        static MortgageWidgetController()
        {
            if (apiWebService == null)
            {
                lock (syncInit)
                {
                    if (apiWebService == null)
                    {
                        apiWebService = Global.ClientWebServices?.FirstOrDefault(x => x.PartnerId == loanTekPartnerId && x.WebServiceName == webServiceName);
                        if (apiWebService == null)
                            throw new NoNullAllowedException("Fatal Error @ Clients." + typeof(MortgageWidgetController).GetCustomAttribute<RoutePrefixAttribute>() + " ApiWebService data is missing.");
                    }
                }
            }
            jsonSerializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = Common.Global.JsonSettings.ReferenceLoopHandling,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
            jsonSerializerSettings.Converters.Add(new StringEnumConverter()); //convert enums from 'int' to 'string'
        }

        /// <summary>
        /// Used to query information about this particular web service. 
        /// </summary>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <returns cref="WebService">WebService object containing details about this web service.</returns>
        [Route("Info")]
        [HttpGet]
        public new HttpResponseMessage Info(string authToken)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new WebService(this.ControllerContext.Request.RequestUri?.AbsoluteUri, ApiWebService));
        }

        /// <summary>   
        /// For use with the LoanTek 'Quote' Widget. Return mortgage rates for a passed in mortgage request. 
        /// </summary>
        /// <remarks>
        /// LoanTek Clients are required to have an active and auto-quoting enabled LoanTek Channel in order to view rates. A Widget License may also be required.
        /// </remarks>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <param name="request" cref="FullMortgageRequest">The mortgage request that contains the personal details about the mortgage.</param>
        /// <returns cref="FullMortgageResponse">On success a FullMortgageResponse object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">FullMortgageResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid Client or User Id)</response>
        [EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin)] //enables Rate Limits
        [Route("Quotes")]
        [HttpPost]
        //[ResponseType(typeof(FullMortgageResponse<IQuoteSubmission<MortgageLoanQuote>>))]
        public HttpResponseMessage FullMortgageRequestQuotes(string authToken, FullMortgageRequest request)
        {
            this.Stopwatch.Start();
            this.AppType = Types.Api.AppType.Widget_Quote;
            const string endPoint = "Quotes";

            HttpResponseMessage errorResponse = this.SetApiAndEndPoint(ApiWebService, endPoint);
            if (errorResponse != null)
                return errorResponse;

            errorResponse = this.CommonChecks(request, endPoint);
            if (errorResponse != null)
                return errorResponse;

            this.AuthToken = (!string.IsNullOrEmpty(authToken)) ? new AuthToken(authToken) : null;
            errorResponse = this.Authorize(ApiWebService.WebServiceName, this.AuthToken, request?.UserId ?? 0);
            if (errorResponse != null)
                return errorResponse;

            // ReSharper disable once PossibleNullReferenceException //a null check on this variable happens in the 'CommonChecks'
            request.CustomQuoteResponseJson = null;

            if (request.Form.QuotingChannelType == QuotingChannelType.NotSpecified)
                request.Form.QuotingChannelType = Partner.DefaultQuotingChannelType;
            if (request.Form.ProductFamilyTypes == null)
                request.Form.ProductFamilyTypes = new List<ProductFamilyType>() { ProductFamilyType.CONVENTIONAL };
            if (request.Form.ProductFamilyTypes.Any(x => x == ProductFamilyType.VA))
            {
                if (request.Form.VATypeType == VATypeType.NotApplicable)
                    request.Form.VATypeType = VATypeType.RegularMilitary;
            }

            Service service = this.CreateService(request, ApiWebService, endPoint);

            ProcessRequest processor = new ProcessRequest(new Converter("LTWR-", "LTWQ-"), PreQualUsers.Instance, Cache.Instance);
            return this.ProcessMortgageRequest(processor, request, service);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin)] //enables Rate Limits
        [Route("~/MortgageWidget/{authToken}/Rates")]
        [HttpPost]
        public HttpResponseMessage OldRoute(string authToken, Models.Mortgage.MortgageApiRequest request)
        {
            FullMortgageRequest req = new FullMortgageRequest();
            req.PassThroughItems = request.PassThroughItems;
            req.ClientDefinedId = request.ClientDefinedIdentifier;
            req.UserId = request.UserId;
            req.CustomQuoteResponseJson = request.CustomQuoteResponseJson;
            req.ConvertFromMortgageLoanRequest(request.LoanRequest);
            return this.FullMortgageRequestRates(authToken, req);
        }

        /// <summary>   
        /// For use with the LoanTek 'Rate' Widget. Return mortgage rates for a passed in mortgage request. 
        /// </summary>
        /// <remarks>
        /// LoanTek Clients are required to have an active and auto-quoting enabled LoanTek Channel in order to view rates. A Widget License may also be required.
        /// </remarks>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <param name="request" cref="FullMortgageRequest">The mortgage request that contains the personal details about the mortgage.</param>
        /// <returns cref="FullMortgageResponse">On success a FullMortgageResponse object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">FullMortgageResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid Client or User Id)</response>
        [EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin)] //enables Rate Limits
        [Route("Rates")]
        [HttpPost]
        public HttpResponseMessage FullMortgageRequestRates(string authToken, FullMortgageRequest request)
        {
            this.Stopwatch.Start();
            this.AppType = Types.Api.AppType.Widget_Rate;
            const string endPoint = "Rates";

            HttpResponseMessage errorResponse = this.SetApiAndEndPoint(ApiWebService, endPoint);
            if (errorResponse != null)
                return errorResponse;

            errorResponse = this.CommonChecks(request, endPoint);
            if (errorResponse != null)
                return errorResponse;

            this.AuthToken = (!string.IsNullOrEmpty(authToken)) ? new AuthToken(authToken) : null;
            errorResponse = this.Authorize(ApiWebService.WebServiceName, this.AuthToken, request?.UserId ?? 0);
            if (errorResponse != null)
                return errorResponse;

            // ReSharper disable once PossibleNullReferenceException
            if (string.IsNullOrEmpty(request.CustomQuoteResponseJson))
                request.CustomQuoteResponseJson = "{\"QuoteId\":\"\",\"APR\":0.1,\"InterestRate\":0.1,\"TermInMonths\":1,\"ProductFamilyType\":\" \",\"ProductClassType\":\" \",\"ProductTermType\":\" \",\"ProductTypeType\":\" \",\"QuoteTypeType\":\" \",\"CalcPrice\":1,\"FinalFees\":1,\"PIP\":1}";
            if (request.Form.QuotingChannelType == QuotingChannelType.NotSpecified)
                request.Form.QuotingChannelType = Partner.DefaultQuotingChannelType;
            if (request.Form.ProductFamilyTypes == null)
                request.Form.ProductFamilyTypes = new List<ProductFamilyType>() { ProductFamilyType.CONVENTIONAL };
            //set VATypeType to RegularMilitary if ProductFamilyTypes contains VA...
            if (request.Form.ProductFamilyTypes.Any(x => x == ProductFamilyType.VA))
            {
                if (request.Form.VATypeType == VATypeType.NotApplicable)
                    request.Form.VATypeType = VATypeType.RegularMilitary;
            } 

            Service service = this.CreateService(request, ApiWebService, endPoint);

            ProcessRequest processor = new ProcessRequest(new Converter("LTWR-", "LTWQ-"), PreQualUsers.Instance, Cache.Instance);
            return this.ProcessMortgageRequest(processor, request, service);

            #region DO NOT SAVE Request, Submission, Quote, etc. data...
            /*
            var response = this.ProcessMortgageRequest(processor, request, null);
            if (service != null)
            {
                Task.Run(() =>
                {
                    service.RequestId = processor.Request?.RequestId; //this needs to be updated to ensure the correct 'RequestId'
                    service.EndTime = DateTime.Now;
                    service = new Services(DataContextType.Database).Put(service);
                }).ConfigureAwait(false);
            }
            return response;
            */
            #endregion

        }
    }
}
