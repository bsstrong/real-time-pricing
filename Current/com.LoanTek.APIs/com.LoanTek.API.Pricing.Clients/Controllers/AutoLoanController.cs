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
using com.LoanTek.API.Pricing.Clients.Models;
using com.LoanTek.API.Pricing.Clients.Models.Loan;
using com.LoanTek.Biz.Api.Objects;
using com.LoanTek.Caching;
using com.LoanTek.Forms.Loan;
using com.LoanTek.Master;
using com.LoanTek.Master.Data.LinqDataContexts;
using com.LoanTek.Quoting.Loans;
using com.LoanTek.Quoting.Loans.Auto;
using com.LoanTek.Quoting.Loans.Auto.IData;
using com.LoanTek.Quoting.Loans.Common;
using com.LoanTek.Rules;
using com.LoanTek.Types;
using LoanTek.LoggingObjects;
using LoanTek.Pricing.Engines.V1.Actions;
using LoanTek.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using WebApiThrottle;
using Partner = com.LoanTek.Biz.Pricing.Objects.Partner;

namespace com.LoanTek.API.Pricing.Clients.Controllers
{
    /// <summary>
    /// Client Web service for Quoting Auto APY requests.
    /// </summary>
    //[RoutePrefix(domain url root +"/Loan/{versionId}/AutoLoan/{authToken}")] '(domain url root +"/Loan/{versionId}' is defined as a path on the server, so it is not needed here
    [RoutePrefix("Auto/{authToken}")]
    public class AutoLoanController : AApiController
    {
        #region Static data

        public const string Name = "AutoLoan";
        public const int LoanTekPartnerId = 1;
        public static Partner Partner;
        public static ApiWebService ApiWebService;
        public static Api ApiObject;

        private const string requestIdPrefix = "PCAR"; //Pricing Client Auto Request
        private const string quoteIdPrefix = "PCAQ"; //Pricing Client Auto Quote
        private const long ratePerSec = 10;
        private const long ratePerMin = 600;

        private static readonly List<string> propertiesToNotSerialize = new List<string>() {"Id", "OwnerId", "ParentId", "TierGroupId", "MustMatchAllRules", "Active" };
            
        //Static constructor is used to initialize any static data or to perform a particular action that needs to be preformed once only
        static AutoLoanController()
        {
            Partner = Global.Partners.FirstOrDefault(x => x.Id == LoanTekPartnerId);
            ApiWebService = Global.ClientWebServices.FirstOrDefault(x => x.PartnerId == LoanTekPartnerId && x.WebServiceName == Name);
            if (Partner == null || ApiWebService == null)
                throw new NoNullAllowedException("Fatal Error @ Clients." + Name + ": Partner and/or ApiWebService data is missing for this LoanTekPartnerId:" + LoanTekPartnerId);

            #region API object  

            ApiObject = new Api();
            ApiObject.WebServiceName = ApiWebService.WebServiceName;
            ApiObject.ApiName = typeof(AutoLoanController).FullName;
            ApiObject.RoutePrefix = ApiWebService.CurrentVersion.WebServiceVersionString;
            ApiObject.Route = Name + "/{authToken}";
            ApiObject.RequestObjectType = typeof(LoanApiRequest<LoanRequest, AutoLoanForm>);
            ApiObject.ResponseObjectType = typeof(LoanApiResponse<LoanSubmission<AutoLoanQuote>, LoanRequest>);
            ApiObject.ResponsePostbackObjectType = typeof(LoanSubmission<AutoLoanQuote>);
            ApiObject.ResponseQuoteObjectType = typeof(LoanQuote);
            ApiObject.RateLimited = true;
            ApiObject.Versions = Common.Global.ApiObject?.Versions;

            #endregion

            PreQualUsers.Config = new Quoting.Loans.PreQualUsers.PreQualConfig()
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

        #endregion

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
        [Route("AutoLoanRequest/Info")]
        [HttpGet]
        public HttpResponseMessage AutoLoanRequestInfo(string authToken)
        {
            //string apiEndPoint = "AutoLoanRequest/Info";
            return Request.CreateResponse(HttpStatusCode.OK, new WebService(this.ControllerContext.Request.RequestUri?.AbsoluteUri, ApiWebService));
        }

        /// <summary>
        /// Used for testing the AutoLoanRequest endpoint.
        /// </summary>
        /// <remarks>
        /// LoanTek Clients are required to have an active Tiers &amp; Rates for Autos in the system in order to quote this request.
        /// </remarks>
        /// <param name="authToken">Unique security token required for accessing these web services. A client can get their token from the developers portal.</param>
        /// <returns cref="LoanApiResponse{LoanSubmission, LoanRequest}">On success a LoanApiResponse{LoanSubmission} object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">ClientAutoResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid UserId Id)</response>
        [EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin, PerHour = 1000)] //enables Rate Limits
        [Route("AutoLoanRequest/Test")]
        [HttpGet]   
        [ResponseType(typeof(LoanApiResponse<LoanSubmission<AutoLoanQuote>, LoanRequest>))]
        public HttpResponseMessage AutoLoanRequestTest(string authToken)
        {
            this.Stopwatch.Start();
            string endPoint = "AutoLoanRequest";
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

            LoanApiRequest<LoanRequest, AutoLoanForm> request = new LoanApiRequest<LoanRequest, AutoLoanForm>();
            request.ClientDefinedIdentifier = "Test" + requestIdPrefix + StringUtilities.UniqueId();
            request.ReturnFailedRules = true;
            request.ReturnCopyOfLoanRequestInResponse = true;
            request.LoanRequest = LoanRequest.GetDummy();
            request.LoanRequest.Form.Amount = NullSafe.NullSafeDecimal(parameters.FirstOrDefault(x => x.Key == "Amount").Value, request.LoanRequest.Form.Amount);
            request.LoanRequest.Form.TermInMonths = NullSafe.NullSafeInteger(parameters.FirstOrDefault(x => x.Key == "TermInMonths").Value, request.LoanRequest.Form.TermInMonths);
            request.UserId = this.AUser.Id;

            return this.AutoLoanRequest(authToken, request);
        }

        /// <summary>
        /// Designed for use with LoanTek's Loan Pricer.
        /// </summary>
        [ApiExplorerSettings(IgnoreApi = true)]
        [EnableThrottling(PerSecond = 20, PerMinute = 1200)] //enables Rate Limits
        [Route("AutoLoanRequest/LoanPricer")]
        [HttpPost]
        public virtual HttpResponseMessage ApyLoanPricerRequest(string authToken, LoanApiRequest<LoanRequest, AutoLoanForm> request)
        {
            this.Stopwatch.Start();
            string endPoint = "AutoLoanRequest/LoanPricer";
            this.AppType = Types.Api.AppType.LoanPricer;

            var errorResponse = this.SetApiAndEndPoint(ApiWebService, endPoint);
            if (errorResponse != null)
                return errorResponse;

            request.LoanRequest.Form.QuotingChannelType = QuotingChannelType.LoanTek; //ALL widget requests should be to LoanTek channel

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
        [Route("AutoLoanRequest/Widget")]
        [HttpPost]
        public virtual HttpResponseMessage AutoLoanWidgetRequest(string authToken, LoanApiRequest<LoanRequest, AutoLoanForm> request)
        {
            this.Stopwatch.Start();
            string endPoint = "AutoLoanRequest/Widget";
            this.AppType = Types.Api.AppType.Widget;

            var errorResponse = this.SetApiAndEndPoint(ApiWebService, endPoint);
            if (errorResponse != null)
                return errorResponse;

            request.LoanRequest.Form.QuotingChannelType = QuotingChannelType.LoanTek; //ALL widget requests should be to LoanTek channel

            ProcessRequest processor = new ProcessRequest(new Converter(this.AppType.ToString()[0] + requestIdPrefix, this.AppType.ToString()[0] + quoteIdPrefix), PreQualUsers.Instance, Cache.Instance);
            HttpResponseMessage response = this.processRequest(processor, authToken, request, endPoint);

            Task.Run(() => this.CreateAndSaveService(response.StatusCode, processor));

            return response;
        }

        /// <summary>
        /// Generate personalized auto loan quotes from active and enabled LoanTek Channel users/clients for a passed in loan request. 
        /// </summary>
        /// <remarks>
        /// LoanTek Clients are required to have an active Tiers &amp; Rates for Autos in the system in order to quote this request.
        /// </remarks>
        /// <param name="authToken">Unique security token required for accessing these web services. A client can get their token from the developers portal.</param>
        /// <param name="request" cref="LoanApiRequest{LoanRequest, AutoLoanForm}">The Loan request that contains the details need to price or provide Apr details.</param>
        /// <returns cref="LoanApiResponse{LoanSubmission, LoanRequest}">On success a LoanApiResponse{LoanSubmission} object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">ClientAutoResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid UserId Id)</response>
        [EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin)] //enables Rate Limits
        [Route("AutoLoanRequest")]
        [HttpPost]
        [ResponseType(typeof(LoanApiResponse<LoanSubmission<AutoLoanQuote>, LoanRequest>))]
        public virtual HttpResponseMessage AutoLoanRequest(string authToken, LoanApiRequest<LoanRequest, AutoLoanForm> request)
        {
            this.Stopwatch.Start();
            string endPoint = "AutoLoanRequest";   
            this.AppType = Types.Api.AppType.Api;

            var errorResponse = this.SetApiAndEndPoint(ApiWebService, endPoint);
            if (errorResponse != null)
                return errorResponse;

            ProcessRequest processor = new ProcessRequest(new Converter(this.AppType.ToString()[0] + requestIdPrefix, this.AppType.ToString()[0] + quoteIdPrefix), PreQualUsers.Instance, Cache.Instance);
            HttpResponseMessage response = this.processRequest(processor, authToken, request, endPoint);

            Task.Run(() => this.CreateAndSaveService(response.StatusCode, processor));

            return response;
        }

        private HttpResponseMessage processRequest(ProcessRequest processor, string authToken, LoanApiRequest<LoanRequest, AutoLoanForm> request, string apiEndPoint)
        {
            try
            {
                DateTime startTime = DateTime.Now;

                HttpResponseMessage errorResponse = this.CommonChecks(request, apiEndPoint);
                if (errorResponse != null)
                    return errorResponse;
        
                Debug.WriteLine("NEW processRequest: LoanApiRequest: " + JsonConvert.SerializeObject(request));
                request.LoanRequest.RequestId = request.ClientDefinedIdentifier; //this temp. set so that the ClientDefinedIdentifier can be passed to the data context 'Request' object to be saved

                AuthToken authTokenObject = (!string.IsNullOrEmpty(authToken)) ? new AuthToken(authToken) : null;
                errorResponse = this.Authorize(apiEndPoint, authTokenObject, request.UserId);
                if (errorResponse != null)
                    return errorResponse;

                this.CommonProcesses(Request);
                CommonParams.UseOnlyThisUserId = request.UserId; //always use a single UserId for client requests

                CancellationTokenSource cancelToken = (Debugger.IsAttached) ? null : this.StartTimeoutTimer(CommonParams.TimeoutInMill);

                processor.Process(request.LoanRequest, JsonConvert.SerializeObject(request), cancelToken, CommonParams.UseOnlyThisUserId); //Types.Processing.DebugModeType.None);

                LoanApiResponse<LoanSubmission<AutoLoanQuote>, LoanRequest> response = new LoanApiResponse<LoanSubmission<AutoLoanQuote>, LoanRequest>();
                response.TimeStamp = DateAndTime.ConvertToUnixTime(DateTime.Now);
                response.Status = processor.Request.StatusType;
                response.LoanTekDefinedIdentifier = processor.Request.RequestId;
                response.CachedId = processor.Request.CachedId;
                response.Submissions = new List<LoanSubmission<AutoLoanQuote>>();
                if (processor.SubmissionsList.ToList().Count > 0)
                    processor.SubmissionsList.ToList().ForEach(x => response.Submissions.Add(x.GetLoanSubmission<LoanSubmission<AutoLoanQuote>>()));
                response.Message = (processor.Request.Misc?.Count > 0) ? string.Join("|", processor.Request.Misc) : null;
                response.ApiEndPoint = apiEndPoint;
                response.ClientDefinedIdentifier = request.ClientDefinedIdentifier;
                response.PassThroughItems = request.PassThroughItems;
                if (request.ReturnCopyOfLoanRequestInResponse)
                    response.LoanRequest = request.LoanRequest;
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
                response.ExecutionTimeInMillisec = Math.Round((DateTime.Now - startTime).TotalMilliseconds, 4);

                JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = Common.Global.JsonSettings.ReferenceLoopHandling,
                    NullValueHandling = NullValueHandling.Include,
                    Formatting = Formatting.None,
                    DefaultValueHandling = DefaultValueHandling.Include
                };
                jsonSerializerSettings.ContractResolver = !string.IsNullOrEmpty(request.CustomQuoteResponseJson) ? new DynamicContractResolver(this.GetPropertiesToSerialize<LoanQuote>(request.CustomQuoteResponseJson), propertiesToNotSerialize) : new DynamicContractResolver(null, propertiesToNotSerialize);
                jsonSerializerSettings.Converters.Add(new StringEnumConverter()); //convert enums from 'int' to 'string'

                if (Debugger.IsAttached)
                {
                    foreach (var timeObj in processor.Request.Times)
                    {
                        Debug.WriteLine("autoloan processRequest execution time in milli secs:" + timeObj.Status + " " + (timeObj.EndTime - timeObj.StartTime).TotalMilliseconds.ToString("##00"));
                    }
                }

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

        /// <summary>
        /// Use to limit or remove unwanted Json properties / data
        /// </summary>
        public class DynamicContractResolver : DefaultContractResolver
        {
            private IList<string> _propertiesToSerialize;
            private IList<string> _propertiesToNotSerialize;

            public DynamicContractResolver(IList<string> propertiesToSerialize, IList<string> propertiesToNotSerialize)
            {
                _propertiesToSerialize = propertiesToSerialize;
                _propertiesToNotSerialize = propertiesToNotSerialize;
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                IEnumerable<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
                if (_propertiesToSerialize?.Count > 0 && (type == typeof(LoanQuote) || type.GetInterfaces().Any(x => x == typeof(ILoanQuote))))
                {
                    properties = properties.Where(p => _propertiesToSerialize.Contains(p.PropertyName));
                }
                return properties.Where(p => _propertiesToNotSerialize.All(x => x != p.PropertyName)).ToList();
            }
        }
    }
}
