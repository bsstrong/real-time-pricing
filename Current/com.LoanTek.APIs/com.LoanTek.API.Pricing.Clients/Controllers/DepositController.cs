using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using com.LoanTek.API.Common.Models;
using com.LoanTek.API.Pricing.Clients.Models;
using com.LoanTek.API.Pricing.Clients.Models.Deposit;
using com.LoanTek.Biz.Api.Objects;
using com.LoanTek.Caching;
using com.LoanTek.Master;
using com.LoanTek.Master.Data.LinqDataContexts;
using com.LoanTek.Quoting.Deposits;
using com.LoanTek.Quoting.Deposits.Common;
using com.LoanTek.Quoting.Deposits.IData;
using com.LoanTek.Rules;
using com.LoanTek.Types;
using LoanTek.LoggingObjects;
using LoanTek.Pricing.Engines.V1.Actions;
using LoanTek.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WebApiThrottle;
using Partner = com.LoanTek.Biz.Pricing.Objects.Partner;

namespace com.LoanTek.API.Pricing.Clients.Controllers
{
    /// <summary>
    /// Client Web service for Quoting Deposit APY requests.
    /// </summary>
    //[RoutePrefix(domain url root +"/Deposit/{versionId}/DepositYield/{authToken}")] '(domain url root +"/Deposit/{versionId}' is defined as a path on the server, so it is not needed here
    [RoutePrefix("DepositYield/{authToken}")]
    public class DepositYieldController : AApiController
    {
        public const string Name = "DepositYield";
        public const int LoanTekPartnerId = 1;
        public static Partner Partner;
        public static ApiWebService ApiWebService;
        public static Api ApiObject;

        private const string requestIdPrefix = "PCDR";
        private const string quoteIdPrefix = "PCDQ";
        private const long ratePerSec = 10;
        private const long ratePerMin = 600;

        //Static constructor is used to initialize any static data or to perform a particular action that needs to be preformed once only
        static DepositYieldController()
        {
            Partner = Global.Partners.FirstOrDefault(x => x.Id == LoanTekPartnerId);
            ApiWebService = Global.ClientWebServices.FirstOrDefault(x => x.PartnerId == LoanTekPartnerId && x.WebServiceName == Name);
            if (Partner == null || ApiWebService == null)
                throw new NoNullAllowedException("Fatal Error @ Clients." + Name + ": Partner and/or ApiWebService data is missing for this LoanTekPartnerId:" + LoanTekPartnerId);

            #region API object  

            ApiObject = new Api();
            ApiObject.WebServiceName = ApiWebService.WebServiceName;
            ApiObject.ApiName = typeof(DepositYieldController).FullName;
            ApiObject.RoutePrefix = ApiWebService.CurrentVersion.WebServiceVersionString;
            ApiObject.Route = Name + "/{authToken}";
            ApiObject.RequestObjectType = typeof(DepositApiRequest);
            ApiObject.ResponseObjectType = typeof(DepositApiResponse);
            ApiObject.ResponsePostbackObjectType = typeof(DepositSubmission);
            ApiObject.ResponseQuoteObjectType = typeof(DepositQuote);
            ApiObject.RateLimited = true;
            ApiObject.Versions = Common.Global.ApiObject?.Versions;

            #endregion

            PreQualUsers.Config = new Quoting.Deposits.PreQualUsers.PreQualConfig()
            {
                QuoteSysDataContextConnStr = new QuoteSystemsDataContext().Connection.ConnectionString,
                LoanTekDataContextConnStr = new LoanTekDataContext().Connection.ConnectionString,
                UpdateDataCheckInterval = TimeSpan.FromMinutes(5)
            };
            Cache.Config = new CacheConfig()
            {
                ExpirationModeType = ExpirationModeType.Absolute,
                ExpirationTime = TimeSpan.FromMinutes(120),
                InvalidateCheckInterval = null
            };
        }

        /// <summary>
        /// Used to query information about this particular web service. 
        /// </summary>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <returns cref="WebService">WebService object containing details about this web service.</returns>
        [Route("Info")]
        [HttpGet]
        public HttpResponseMessage Info(string authToken)
        {
            return Request.CreateResponse(HttpStatusCode.OK, new WebService(this.ControllerContext.Request.RequestUri?.AbsoluteUri, ApiWebService));
        }

        /// <summary>
        /// Used to query information about this particular web service. 
        /// </summary>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <returns cref="WebService">WebService object containing details about this web service.</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [Obsolete]
        [Route("ApyRequest/Info")]
        [HttpGet]
        public HttpResponseMessage ApyRequestInfo(string authToken)
        {
            //string apiEndPoint = "AutoLoanRequest/Info";
            return Request.CreateResponse(HttpStatusCode.OK, new WebService(this.ControllerContext.Request.RequestUri?.AbsoluteUri, ApiWebService));
        }

        /// <summary>
        /// Used for testing the ApyRequest endpoint.
        /// </summary>
        /// <remarks>
        /// LoanTek Clients are required to have an active Tiers &amp; Rates for Deposits in the system in order to quote this request.
        /// </remarks>
        /// <param name="authToken">Unique security token required for accessing these web services. A client can get their token from the developers portal.</param>
        /// <returns cref="DepositApiResponse">On success a ClientDepositResponse object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">ClientDepositResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid UserId Id)</response>
        [EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin, PerHour = 1000)] //enables Rate Limits
        [Route("ApyRequest/Test")]
        [HttpGet]   
        [ResponseType(typeof(DepositApiResponse))]
        public HttpResponseMessage ApyRequestTest(string authToken)
        {
            this.Stopwatch.Start();
            string endPoint = "ApyRequest/Test";
            this.AppType = Types.Api.AppType.Api;

            var errorResponse = this.SetApiAndEndPoint(ApiWebService, endPoint);
            if (errorResponse != null)
                return errorResponse;

            var parameters = this.Request.GetQueryNameValuePairs().ToList();
            int userId = NullSafe.NullSafeInteger(parameters.FirstOrDefault(x => x.Key == "UserId").Value, 0);

            AuthToken authTokenObject = (!string.IsNullOrEmpty(authToken)) ? new AuthToken(authToken) : null;
            errorResponse = this.Authorize(ApiWebService.WebServiceName, authTokenObject, userId);
            if (errorResponse != null)
                return errorResponse;

            DepositApiRequest request = new DepositApiRequest();
            request.ClientDefinedIdentifier = "Test" + StringUtilities.UniqueId();
            request.ReturnFailedRules = true;
            request.ReturnCopyOfDepositRequestInResponse = true;
            request.DepositRequest = DepositRequest.GetDummy();
            request.DepositRequest.Form.Amount = NullSafe.NullSafeDecimal(parameters.FirstOrDefault(x => x.Key == "Amount").Value, request.DepositRequest.Form.Amount);
            request.DepositRequest.Form.TermInMonths = NullSafe.NullSafeInteger(parameters.FirstOrDefault(x => x.Key == "TermInMonths").Value, request.DepositRequest.Form.TermInMonths);
            request.DepositRequest.Form.ForType = parameters.FirstOrDefault(x => x.Key == "ForType").Value != null ? EnumLib.Parse<TypesForRates.ForType>(parameters.FirstOrDefault(x => x.Key == "ForType").Value) : TypesForRates.ForType.DepositCd;
            request.UserId = this.AUser.Id;

            return this.ApyRequest(authToken, request);
        }

        /// <summary>
        /// Designed for use with LoanTek's Loan Pricer.
        /// </summary>
        [ApiExplorerSettings(IgnoreApi = true)]
        [EnableThrottling(PerSecond = 20, PerMinute = 1200)] //enables Rate Limits
        [Route("ApyRequest/LoanPricer")]
        [HttpPost]
        public virtual HttpResponseMessage ApyLoanPricerRequest(string authToken, DepositApiRequest request)
        {
            this.Stopwatch.Start();

            this.EndPoint = ApiWebService.GetEndPoints().FirstOrDefault(x => x.EndPoint == "ApyRequest");
            string endPoint = this.EndPoint?.EndPoint ?? "ApyRequest/LoanPricer";
            if (this.EndPoint == null || ApiWebService == null)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing Web Service / Endpoint", endPoint);
            if (this.EndPoint.VersionStatusType == Types.Api.VersionStatusType.Removed || ApiWebService.ApiStatusType == Types.Api.ApiStatusType.Inactive)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "In-active Web Service / Endpoint", endPoint);

            this.AppType = Types.Api.AppType.LoanPricer;

            request.DepositRequest.Form.QuotingChannelType = QuotingChannelType.LoanTek; //ALL widget requests should be to LoanTek channel

            bool doNotSaveToDataContext = false;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            ProcessRequest processor = new ProcessRequest(new Converter(this.AppType.ToString()[0] + requestIdPrefix, this.AppType.ToString()[0] + quoteIdPrefix), PreQualUsers.Instance, Cache.Instance, doNotSaveToDataContext);
            HttpResponseMessage response = this.processRequest(processor, authToken, request, endPoint);

            //Task.Run(() => this.CreateAndSaveService(response.StatusCode, processor));

            return response;
        }

        /// <summary>
        /// Designed for use with LoanTek widgets.
        /// </summary>
        [ApiExplorerSettings(IgnoreApi = true)]
        [EnableThrottling(PerSecond = 20, PerMinute = 1200)] //enables Rate Limits
        [Route("ApyRequest/Widget")]
        [HttpPost]
        public virtual HttpResponseMessage ApyWidgetRequest(string authToken, DepositApiRequest request)
        {
            this.Stopwatch.Start();
            string endPoint = "ApyRequest/Widget";
            this.AppType = Types.Api.AppType.Api;

            var errorResponse = this.SetApiAndEndPoint(ApiWebService, endPoint);
            if (errorResponse != null)
                return errorResponse;

            request.DepositRequest.Form.QuotingChannelType = QuotingChannelType.LoanTek; //ALL widget requests should be to LoanTek channel

            ProcessRequest processor = new ProcessRequest(new Converter(this.AppType.ToString()[0] + requestIdPrefix, this.AppType.ToString()[0] + quoteIdPrefix), PreQualUsers.Instance, Cache.Instance);
            HttpResponseMessage response = this.processRequest(processor, authToken, request, endPoint);

            Task.Run(() => this.CreateAndSaveService(response.StatusCode, processor));

            return response;
        }

        /// <summary>
        /// Generate personalized deposit quotes from active and enabled LoanTek Channel users/clients for a passed in deposit request. 
        /// </summary>
        /// <remarks>
        /// LoanTek Clients are required to have an active Tiers &amp; Rates for Deposits in the system in order to quote this request.
        /// </remarks>
        /// <param name="authToken">Unique security token required for accessing these web services. A client can get their token from the developers portal.</param>
        /// <param name="request" cref="DepositApiRequest">The deposit request that contains the details need to price or provide Apy details.</param>
        /// <returns cref="DepositApiResponse">On success a ClientDepositResponse object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">ClientDepositResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid UserId Id)</response>
        [EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin)] //enables Rate Limits
        [Route("ApyRequest")]
        [HttpPost]
        [ResponseType(typeof(DepositApiResponse))]
        public virtual HttpResponseMessage ApyRequest(string authToken, DepositApiRequest request)
        {
            this.Stopwatch.Start();
            string endPoint = "ApyRequest";
            this.AppType = Types.Api.AppType.Api;

            var errorResponse = this.SetApiAndEndPoint(ApiWebService, endPoint);
            if (errorResponse != null)
                return errorResponse;

            ProcessRequest processor = new ProcessRequest(new Converter(this.AppType.ToString()[0] + requestIdPrefix, this.AppType.ToString()[0] + quoteIdPrefix), PreQualUsers.Instance, Cache.Instance);
            HttpResponseMessage response = this.processRequest(processor, authToken, request, endPoint);

            Task.Run(() => this.CreateAndSaveService(response.StatusCode, processor));

            return response;
        }

        private HttpResponseMessage processRequest(ProcessRequest processor, string authToken, DepositApiRequest request, string apiEndPoint)
        {
            try
            {
                DateTime startTime = DateTime.Now;

                HttpResponseMessage errorResponse = this.CommonChecks(request, apiEndPoint);
                if (errorResponse != null)
                    return errorResponse;

                Debug.WriteLine("NEW processRequest: DepositApiRequest: " + JsonConvert.SerializeObject(request));
                request.DepositRequest.RequestId = request.ClientDefinedIdentifier; //this temp. set so that the ClientDefinedIdentifier can be passed to the data context 'Request' object to be saved

                AuthToken authTokenObject = (!string.IsNullOrEmpty(authToken)) ? new AuthToken(authToken) : null;
                errorResponse = this.Authorize(ApiWebService.WebServiceName, authTokenObject, request.UserId);
                if (errorResponse != null)
                    return errorResponse;

                this.CommonProcesses(Request);
                CommonParams.UseOnlyThisUserId = request.UserId; //always use a single UserId for client requests

                if (string.IsNullOrEmpty(request.DepositRequest.RequestId))
                    request.DepositRequest.RequestId = request.ClientDefinedIdentifier;

                CancellationTokenSource cancelToken = (Debugger.IsAttached) ? null : this.StartTimeoutTimer(CommonParams.TimeoutInMill);

                processor.Process(request.DepositRequest, JsonConvert.SerializeObject(request), cancelToken, CommonParams.UseOnlyThisUserId); //Types.Processing.DebugModeType.None);

                DepositApiResponse response = new DepositApiResponse();
                response.TimeStamp = DateAndTime.ConvertToUnixTime(DateTime.Now);
                response.Status = processor.Request.StatusType;
                response.LoanTekDefinedIdentifier = processor.Request.RequestId;
                response.CachedId = processor.Request.CachedId;
                response.Submissions = processor.Converter.SentSubmissions;
                response.Message = string.Join("|", processor.Request.Misc);
                response.ApiEndPoint = apiEndPoint;
                response.ClientDefinedIdentifier = request.ClientDefinedIdentifier;
                response.PassThroughItems = request.PassThroughItems;
                if (request.ReturnCopyOfDepositRequestInResponse)
                    response.DepositRequest = request.DepositRequest;
                #region ReturnFailedRules
                if (request.ReturnFailedRules)
                {
                    if (response.AdditionalItems == null)
                        response.AdditionalItems = new List<KeyValuePair<string, object>>();
                    List<RuleCheckResponse> failedRules = new List<RuleCheckResponse>();
                    processor.PricingEngine?.FindMatchingClasses?.FirstOrDefault(x => x is FindMatchingRates)?.FailedRulesByTierBranch.Values.ForEach(xx => failedRules.AddRange(xx));
                    if (failedRules.Count > 0)
                        response.AdditionalItems.Add(new KeyValuePair<string, object>("FailedRequirementRules", failedRules));

                    List<RuleCheckResponse> failedAdjRules = new List<RuleCheckResponse>();
                    processor.PricingEngine?.FindMatchingClasses?.FirstOrDefault(x => x is FindMatchingRates)?.FailedRulesByTierBranch.Values.ForEach(xx => failedAdjRules.AddRange(xx));
                    if (failedAdjRules.Count > 0)
                        response.AdditionalItems.Add(new KeyValuePair<string, object>("FailedAdjustmentRules", failedAdjRules));

                    List<RuleCheckResponse> failedCapRules = new List<RuleCheckResponse>();
                    processor.PricingEngine?.FindMatchingClasses?.FirstOrDefault(x => x is FindMatchingRates)?.FailedRulesByTierBranch.Values.ForEach(xx => failedCapRules.AddRange(xx));
                    if (failedCapRules.Count > 0)
                        response.AdditionalItems.Add(new KeyValuePair<string, object>("FailedCapRules", failedCapRules));

                    List<RuleCheckResponse> failedFeeRules = new List<RuleCheckResponse>();
                    processor.PricingEngine?.FindMatchingClasses?.FirstOrDefault(x => x is FindMatchingRates)?.FailedRulesByTierBranch.Values.ForEach(xx => failedFeeRules.AddRange(xx));
                    if (failedFeeRules.Count > 0)
                        response.AdditionalItems.Add(new KeyValuePair<string, object>("FailedClientFeeRules", failedFeeRules));
                }
                #endregion

                foreach (var timeObj in processor.Request.Times)
                {
                    Debug.WriteLine(timeObj.Status + " " + (timeObj.EndTime - timeObj.StartTime).TotalMilliseconds);
                }


                response.ExecutionTimeInMillisec = Math.Round((DateTime.Now - startTime).TotalMilliseconds, 4);

                JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = Common.Global.JsonSettings.ReferenceLoopHandling,
                    NullValueHandling = NullValueHandling.Include,
                    Formatting = Formatting.None,
                    DefaultValueHandling = DefaultValueHandling.Include
                };
                if(!string.IsNullOrEmpty(request.CustomQuoteResponseJson))
                    jsonSerializerSettings.ContractResolver = new DynamicContractResolver<DepositQuote,IDepositQuote>(this.GetPropertiesToSerialize<DepositQuote>(request.CustomQuoteResponseJson));
                jsonSerializerSettings.Converters.Add(new StringEnumConverter()); //convert enums from 'int' to 'string'

                return Request.CreateResponse(HttpStatusCode.OK, response, new JsonMediaTypeFormatter() { SerializerSettings = jsonSerializerSettings });
            }
            catch (Exception ex)
            {
                Global.OutPrint(ex.Message, new SimpleLogger.LocationObject(this, "MortgageRequest/POST"), SimpleLogger.LogLevelType.CRITICAL);
                return this.CreateErrorResponse(HttpStatusCode.InternalServerError, "Exception:" + ex.Message, apiEndPoint);
            }
        }

        /// <summary>
        /// Create and Save the 'Service' data object
        /// </summary>
        [ApiExplorerSettings(IgnoreApi = true)]
        protected Service CreateAndSaveService(HttpStatusCode statusCode, ProcessRequest processor)
        {
            Service obj = new Service();
            obj.RequestId = processor.Request?.RequestId;
            obj.StartTime = DateTime.Now.AddMilliseconds(-Stopwatch.ElapsedMilliseconds);
            obj.EndTime = DateTime.Now;
            obj.UserId = processor.Request?.UserId ?? 0;
            obj.ClientId = this.AuthToken?.ClientId > 0 ? this.AuthToken.ClientId : Users.GetUserById(obj.UserId)?.ClientId ?? 0;
            obj.ApiWebServiceId = ApiWebService.Id;
            obj.ServiceName = ApiWebService.WebServiceName;
            obj.Endpoint = this.EndPoint?.EndPoint;
            obj.Route = this.Request?.RequestUri?.PathAndQuery;
            obj.HttpStatusCodeType = statusCode;
            obj.Message = (processor.Request?.Misc?.Count > 0) ? string.Join("|", processor.Request.Misc) : null;
            obj.CallingIpAddress = ClientInfo.GetIPAddress(this.Request);
            obj.CallingAppType = this.AppType;
            var response = new Services(DataConnections.DataContextType, DataConnections.DataContextQuoteSystemsWrite).PutWithResponse(obj);
            return response.Success ? response.DataObject : null;
        }

        
    }
}
