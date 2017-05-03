using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Description;
using com.LoanTek.API.Common.Filters;
using com.LoanTek.API.Instances;
using com.LoanTek.API.Pricing.Partners.Models;
using com.LoanTek.API.Pricing.Partners.Models.Common;
using com.LoanTek.API.Requests;
using com.LoanTek.Biz.Api.Objects;
using com.LoanTek.Caching;
using com.LoanTek.Master;
using com.LoanTek.Master.Data.LinqDataContexts;
using com.LoanTek.Quoting;
using com.LoanTek.Quoting.Common;
using com.LoanTek.Quoting.Core;
using com.LoanTek.Types;
using LoanTek.LoggingObjects;
using LoanTek.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WebApiThrottle;
using FullMortgageRequest = com.LoanTek.API.Pricing.Clients.Models.Mortgage.MortgageApiRequest;
using MortgageLoanRequest = com.LoanTek.API.Requests.MortgageLoanRequest;
using Partner = com.LoanTek.Biz.Pricing.Objects.Partner;
using PreQualUsers = com.LoanTek.Quoting.LegacyClient.PreQualUsers;
using Cache = com.LoanTek.Quoting.Common.Cache;

namespace com.LoanTek.API.Pricing.Clients.Controllers
{
    /// <summary>
    /// Client Web service for Quoting Mortgage Loan requests.
    /// </summary>
    //[RoutePrefix(domain url root +"/Mortgage/{versionId}/MortgageLoan/{authToken}")] '(domain url root +"/Mortgage/{versionId}' is defined as a path on the server, so it is not needed here
    [RoutePrefix("MortgageLoan/{authToken}")]
    public class MortgageLoanController : Models.AApiController
    {
        public const string Name = "MortgageLoan";
        public const int LoanTekPartnerId = 1;
        public static Partner Partner;
        public static ApiWebService ApiWebService;

        private const long ratePerSec = 10;
        private const long ratePerMin = 600;

        //Static constructor is used to initialize any static data or to perform a particular action that needs to be preformed once only
        static MortgageLoanController()
        {
            Partner = Global.Partners.FirstOrDefault(x => x.Id == LoanTekPartnerId);
            ApiWebService = Global.ClientWebServices.FirstOrDefault(x => x.PartnerId == LoanTekPartnerId && x.WebServiceName == Name);
            if (Partner == null || ApiWebService == null)
                throw new NoNullAllowedException("Fatal Error @ Clients."+ Name +": Partner and/or ApiWebService data is missing for this LoanTekPartnerId:" + LoanTekPartnerId);

            PreQualUsers.Config = new PreQualConfig()
            {
                QuoteSysDataContextConnStr = new QuoteSystemsDataContext().Connection.ConnectionString,
                LoanTekDataContextConnStr = new LoanTekDataContext().Connection.ConnectionString,
                UpdateDataCheckInterval = TimeSpan.FromMinutes(4)
            };

            Cache.Config = new RegionCacheConfig();
            Cache.Config.CacheHandleConfigs.Add(new CacheHandleConfig(CacheSystemType.Dictionary, CacheHandleType.InProcess.ToString(), ExpirationModeType.Absolute, TimeSpan.FromMinutes(60)));
            //Cache.Config.CacheHandleConfigs.Add(new CacheHandleConfig.RedisCacheHandleConfig(CacheSystemType.Redis, CacheHandleType.Shared.ToString(), ExpirationModeType.Absolute, TimeSpan.FromMinutes(240))
            //{
            //    ConnectionTimeout = 10000,
            //    Host = "10.83.95.37",
            //    Port = 6379,
            //    Password = "MANTLE7"
            //});
        }

        /// <summary>
        /// Get cache instance data and clear the cache.
        /// </summary>
        /// <param name="clear">Set to True if you want to clear the cache.</param>
        /// <param name="region">Set to the UserId if you want to clear only the cache for a single user.</param>
        /// <returns></returns>
        [IPAddressValidation]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        [Route("Cache")]
        public HttpResponseMessage GetCache(bool clear = false, int region = 0)
        {
            //string apiEndPoint = "Cache";
            if (clear)
            {
                if (region > 0)
                    Cache.Instance.ClearRegion(region);
                else
                    Cache.Instance.ClearAll();
            }
            return Request.CreateResponse(HttpStatusCode.OK, new CacheInstance(this.ControllerContext.Request.RequestUri?.AbsoluteUri, Cache.Instance));
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
        [Route("FullMortgageRequest/Info")]
        [HttpGet]
        public HttpResponseMessage FullMortgageRequestInfo(string authToken)
        {
            //string apiEndPoint = "FullMortgageRequest/Info";
            return Request.CreateResponse(HttpStatusCode.OK, new WebService(this.ControllerContext.Request.RequestUri?.AbsoluteUri, ApiWebService));
        }

        /// <summary>
        /// Used for testing the FullMortgageRequest endpoint.
        /// </summary>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <returns cref="FullMortgageResponse">On success a FullMortgageResponse object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">FullMortgageResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid Client or User Id)</response>
        [EnableThrottling(PerSecond = 1, PerMinute = 10, PerHour = 600)] //enables Rate Limits
        [Route("FullMortgageRequest/Test")]
        [HttpGet] //used for testing
        [ResponseType(typeof(FullMortgageResponse<IQuoteSubmission<MortgageLoanQuote>>))]
        public virtual HttpResponseMessage FullMortgageRequestTest(string authToken)
        {
            this.Stopwatch.Start();
            this.AppType = Types.Api.AppType.Api;
            string endPoint = "FullMortgageRequest/Test";

            var errorResponse = this.SetApiAndEndPoint(ApiWebService, endPoint);
            if (errorResponse != null)
                return errorResponse;

            this.AuthToken = (!string.IsNullOrEmpty(authToken)) ? new AuthToken(authToken) : null;

            FullMortgageRequest request = (this.AuthToken != null) ? this.CreateDummyRequest(this.AuthToken.ClientId) : null;

            bool doNotSaveToDataContext = true; 
            int timeoutInMill = 10000;
            ProcessRequest processor = new ProcessRequest(new Models.Mortgage.Converter(), PreQualUsers.Instance, Cache.Instance);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            HttpResponseMessage response = this.ProcessMortgageRequest(processor, request, this.CreateService(HttpStatusCode.Accepted, request, null, Types.Api.AppType.Api, endPoint), doNotSaveToDataContext, this.StartTimeoutTimer(timeoutInMill));

            return response;
        }

        /// <summary>
        /// Designed for use with BestExe type: JustRemoveStops. Request and Quotes are not saved to the database.
        /// </summary>
        /// <response code="200">FullMortgageResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid Partner Id)</response>
        /// <response code="406">Loan Request parameters are not acceptable. ProductFamily and ProductTerm Types limited to 1.</response>
        [ApiExplorerSettings(IgnoreApi = true)]
        [EnableThrottling(PerSecond = 2, PerMinute = 20)] //enables Rate Limits
        [Route("FullMortgageRequest/JustRemoveStops")]
        [HttpPost]
        public virtual HttpResponseMessage FullMortgageLoanPricerRequest(string authToken, FullMortgageRequest request)
        {
            this.Stopwatch.Start();
            this.AppType = Types.Api.AppType.Api;
            string endPoint = "FullMortgageRequest/JustRemoveStops";

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

            if (this.AuthToken.ClientId == 5 || this.AuthToken.ClientId == 399 || this.AuthToken.ClientId == 848) //848=Jared Martin (2216), Keystone Funding (848)
            {
                bool doNotSaveToDataContext = false;
                int timeoutInMill = 15000;

                //if (request.LoanRequest?.BestExecutionMethod == BestExecutionMethodType.JustRemoveStops)
                {
                    if(request?.LoanRequest?.ProductTerm?.Count > 4)
                        return this.CreateErrorResponse(HttpStatusCode.NotAcceptable, "ProductTerm Types limited to 4", endPoint);
                    if (request?.LoanRequest?.ProductFamily?.Count > 2)
                        return this.CreateErrorResponse(HttpStatusCode.NotAcceptable, "ProductFamily Types limited to 2", endPoint);
                }

                ProcessRequest processor = new ProcessRequest(new Models.Mortgage.Converter(), PreQualUsers.Instance, Cache.Instance);
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                HttpResponseMessage response = this.ProcessMortgageRequest(processor, request, this.CreateService(HttpStatusCode.Accepted, request, null, Types.Api.AppType.Api, endPoint), doNotSaveToDataContext, this.StartTimeoutTimer(timeoutInMill));
                    
                return response;
            }
            return this.CreateErrorResponse(HttpStatusCode.Unauthorized, "Unauthorized. Please contact LoanTek.", endPoint);

        }

        /// <summary>
        /// Generate personalized mortgage quotes from active and enabled LoanTek Channel users/clients for a passed in mortgage request. 
        /// </summary>
        /// <remarks>
        /// LoanTek Clients are required to have an active and auto-quoting enabled LoanTek Channel in order to quote this request.
        /// </remarks>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <param name="request" cref="FullMortgageRequest">The mortgage request that contains the personal details about the mortgage.</param>
        /// <returns cref="FullMortgageResponse">On success a FullMortgageResponse object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">FullMortgageResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid Client or User Id)</response>
        [EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin)] //enables Rate Limits
        [Route("FullMortgageRequest")]
        [HttpPost]
        [ResponseType(typeof(FullMortgageResponse<IQuoteSubmission<MortgageLoanQuote>>))]
        public virtual HttpResponseMessage FullMortgageRequest(string authToken, FullMortgageRequest request)
        {
            this.Stopwatch.Start();
            this.AppType = Types.Api.AppType.Api;
            string endPoint = "FullMortgageRequest";

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

            if (request?.LoanRequest.QuotingChannel == QuotingChannelType.NotSpecified)
                request.LoanRequest.QuotingChannel = Partner.DefaultQuotingChannelType;
            if (request?.LoanRequest.BestExecutionMethod == BestExecutionMethodType.NotSpecified)
                request.LoanRequest.BestExecutionMethod = BestExecutionMethodType.ByPointGroup;
            else if (request?.LoanRequest.BestExecutionMethod == BestExecutionMethodType.JustRemoveStops)
                request.LoanRequest.BestExecutionMethod = BestExecutionMethodType.ByRate;

            ProcessRequest processor = new ProcessRequest(new Models.Mortgage.Converter(), PreQualUsers.Instance, Cache.Instance);
            HttpResponseMessage response = this.ProcessMortgageRequest(processor, request, this.CreateService(HttpStatusCode.Accepted, request, null, Types.Api.AppType.Api, endPoint));

            return response;
        }

        #region Methods

        /// <summary>
        /// Internal method shared by the public endpoints for process ing a request
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="request"></param>
        /// <param name="service"></param>
        /// <param name="doNotInsert"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        protected HttpResponseMessage ProcessMortgageRequest(ProcessRequest processor, FullMortgageRequest request, Service service, bool doNotInsert = false, CancellationTokenSource cancelToken = null)
        {
            try
            {
                Debug.WriteLine("NEW processMortgageRequest: " + request.ClientDefinedIdentifier +" for UserId: "+ request.UserId);

                this.CommonProcesses(Request);

                FullMortgageResponse response = new FullMortgageResponse();

                if (string.IsNullOrEmpty(request.LoanRequest.ClientDefinedIdentifier))
                    request.LoanRequest.ClientDefinedIdentifier = request.ClientDefinedIdentifier;
                if (request.LoanRequest.BestExecutionMethod == BestExecutionMethodType.NotSpecified)
                    request.LoanRequest.BestExecutionMethod = BestExecutionMethodType.ByPointGroup;

                if(cancelToken == null)
                    cancelToken = this.StartTimeoutTimer(CommonParams.TimeoutInMill);

#pragma warning disable 4014
                processor.Process(service, request.LoanRequest, JsonConvert.SerializeObject(request), cancelToken, request.UserId, CommonParams.DebugModeType, doNotInsert);
#pragma warning restore 4014

                response.TimeStamp = DateAndTime.ConvertToUnixTime(DateTime.Now);
                response.Status = processor.Request?.StatusType ?? Processing.StatusType.Error;
                response.ClientDefinedIdentifier = processor.Request?.RequestId ?? request.ClientDefinedIdentifier;
                response.LoanTekDefinedIdentifier = (processor.Request?.Id > 0) ? processor.Request.Id.ToString() : request.ClientDefinedIdentifier;
                response.CachedId = processor.Request?.CachedId;
                response.Message += processor.Request?.Misc;
                response.ApiEndPoint = service?.Endpoint;
                response.PassThroughItems = request.PassThroughItems;

                var submissionStatusType = processor.Submissions.FirstOrDefault()?.StatusType ?? response.Status;
                if (submissionStatusType == Processing.StatusType.Cancelled || submissionStatusType == Processing.StatusType.Error)
                {
                    response.Status = submissionStatusType;
                    response.Message += "|" + processor.Submissions.FirstOrDefault()?.LoanQuoteSubmissionResult?.Quotes?.FirstOrDefault()?.QuoteId;
                }
                else
                    response.Submissions = processor.Submissions.Select(x => x.LoanQuoteSubmission).ToList();

                response.ExecutionTimeInMillisec = Math.Round((double) this.Stopwatch.ElapsedMilliseconds, 4);

                var jsonSerializerSettings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = Common.Global.JsonSettings.ReferenceLoopHandling,
                    NullValueHandling = NullValueHandling.Include,
                    Formatting = Formatting.None,
                    DefaultValueHandling = DefaultValueHandling.Include
                };
                jsonSerializerSettings.ContractResolver = new DynamicContractResolver(this.GetPropertiesToSerialize(request.CustomQuoteResponseJson));
                jsonSerializerSettings.Converters.Add(new StringEnumConverter()); //convert enums from 'int' to 'string'   

                return Request.CreateResponse(HttpStatusCode.OK, response, new JsonMediaTypeFormatter() { SerializerSettings = jsonSerializerSettings });
            }
            catch (Exception ex)
            {
                Partners.Global.OutPrint(ex.Message, new SimpleLogger.LocationObject(this, "MortgageRequest/POST"), SimpleLogger.LogLevelType.CRITICAL);             
                return this.CreateErrorResponse(HttpStatusCode.InternalServerError, "Exception:" + ex.Message, service?.Endpoint);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        protected Service CreateService(HttpStatusCode statusCode, FullMortgageRequest request, string message, Types.Api.AppType appType, string endPoint)
        {
            Service obj = new Service();
            obj.SetDataContextConnStr(DataConnections.DataContextQuoteSystemsWrite);    
            obj.RequestId = request.ClientDefinedIdentifier;
            obj.StartTime = DateTime.Now.AddMilliseconds(-Stopwatch.ElapsedMilliseconds);
            obj.EndTime = DateTime.Now;
            obj.ClientId = this.AuthToken?.ClientId > 0 ? this.AuthToken.ClientId : Users.GetUserById(request.UserId)?.ClientId ?? 0;
            obj.UserId = request.UserId;
            obj.ApiWebServiceId = ApiWebService.Id;
            obj.ServiceName = ApiWebService.WebServiceName;
            obj.Endpoint = endPoint ?? this.EndPoint?.EndPoint;
            obj.Route = this.Request?.RequestUri?.PathAndQuery;
            obj.HttpStatusCode = statusCode.ToString();
            obj.Message = message;
            obj.CallingIpAddress = ClientInfo.GetIPAddress(this.Request);
            obj.CallingAppType = appType.ToString();
            //obj.Save();
            return obj;
        }

#pragma warning disable 1591

        [ApiExplorerSettings(IgnoreApi = true)]
        public FullMortgageRequest CreateDummyRequest(int clientId)
        {
            if (clientId == 0)
                return null;
            FullMortgageRequest dummyRequest = new FullMortgageRequest();
            dummyRequest.UserId = getPricingUserId(clientId);
            dummyRequest.ClientDefinedIdentifier = "LTC" + StringUtilities.UniqueId();
            dummyRequest.PassThroughItems = new List<object>() {new {Item = "A Pass Through Item"}};
            dummyRequest.LoanRequest = new MortgageLoanRequest();
            ClassMappingUtilities.SetPropertiesForTarget(DummyData.GetRequest(dummyRequest.UserId), dummyRequest.LoanRequest);
            dummyRequest.LoanRequest.QuotingChannel = Partner.DefaultQuotingChannelType;
            dummyRequest.LoanRequest.BestExecutionMethod = BestExecutionMethodType.ByPointGroup;
            return dummyRequest;
        }

        private readonly Dictionary<int, int> pricingUserIds = new Dictionary<int, int>();
        private int getPricingUserId(int clientId)
        {
            if (!this.pricingUserIds.ContainsKey(clientId))
            {
                using (var dc = new LoanTekDataContext())
                {
                    var result = dc.GetAutoPricingLicenseUserId(clientId, 0).FirstOrDefault();
                    if (result != null)
                        this.pricingUserIds.Add(clientId, result.userid);
                }
            }
            return this.pricingUserIds[clientId];
        }

        #endregion

    }
}
