using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using com.LoanTek.API.Common.Filters;
using com.LoanTek.API.Common.Models;
using com.LoanTek.API.Instances;
using com.LoanTek.API.Pricing.Partners.Models;
using com.LoanTek.API.Pricing.Partners.Models.Common;
using com.LoanTek.API.Requests;
using com.LoanTek.Biz.Api.Objects;
using com.LoanTek.Caching;
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
using MortgageLoanRequest = com.LoanTek.API.Requests.MortgageLoanRequest;
using Partner = com.LoanTek.Biz.Pricing.Objects.Partner;
using StatusType = com.LoanTek.Types.Processing.StatusType;

namespace com.LoanTek.API.Pricing.Partners.Controllers
{
    /// <summary>
    /// Partner Web service for Quoting Mortgage Loan requests.
    /// </summary>
    //[RoutePrefix("Pricing/Partner/{versionId}/Mortgage/{partnerName}/{authToken}/{partnerId}")] 'Pricing/Partner/{versionId}' is defined as a path on the server, so it is not needed here
    [RoutePrefix("Mortgage/{partnerName}/{authToken}")]
    public class CommonController : AApiController
    {
        public const string RoutePrefix = "Mortgage/{partnerName}/{authToken}";
        private const long ratePerSec = 10;
        private const long ratePerMin = 600;
        
        //Static constructor is used to initialize any static data or to perform a particular action that needs to be preformed once only
        static CommonController()
        {
            PreQualUsers.Config = new PreQualConfig()
            {
                QuoteSysDataContextConnStr = new QuoteSystemsDataContext().Connection.ConnectionString,
                LoanTekDataContextConnStr = new LoanTekDataContext().Connection.ConnectionString,
                UpdateDataCheckInterval = TimeSpan.FromMinutes(2)
            };

            Cache.Config = new RegionCacheConfig();
            Cache.Config.CacheHandleConfigs.Add(new CacheHandleConfig(CacheSystemType.Dictionary, CacheHandleType.InProcess.ToString(), ExpirationModeType.Absolute, TimeSpan.FromMinutes(2)));
            //Cache.Config.CacheHandleConfigs.Add(new CacheHandleConfig.RedisCacheHandleConfig(CacheSystemType.Redis, CacheHandleType.Shared.ToString(), ExpirationModeType.Absolute, TimeSpan.FromMinutes(10))
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
            return Request.CreateResponse(HttpStatusCode.OK, new CacheInstance(this.ControllerContext?.Request?.RequestUri?.AbsoluteUri, Cache.Instance));
        }

        /// <summary>
        /// Used to query information about this particular web service. 
        /// </summary>
        /// <param name="partnerName">The partner name this request is for.</param>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <returns cref="WebService">WebService object containing details about this web service.</returns>
        [Route("Info")]
        [HttpGet]
        public HttpResponseMessage Info(string partnerName, string authToken)
        {
#pragma warning disable 612
            return this.FullMortgageRequestInfo(partnerName, authToken);
#pragma warning restore 612
        }

        #region Mortgage Requests

        /// <summary>
        /// Used to query information about this particular web service. 
        /// </summary>
        /// <param name="partnerName">The partner name this request is for.</param>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <returns cref="WebService">WebService object containing details about this web service.</returns>
        [ApiExplorerSettings(IgnoreApi = true)]
        [Obsolete]
        [Route("FullMortgageRequest/Info")]
        [HttpGet]
        public HttpResponseMessage FullMortgageRequestInfo(string partnerName, string authToken)
        {
            string apiEndPoint = "FullMortgageRequest/Info";
            Partner partner = Global.Partners.FirstOrDefault(x => x.DefaultQuotingChannelType.ToString().Equals(partnerName, StringComparison.InvariantCultureIgnoreCase));
            if (partner?.Active ?? false)
            {
                ApiWebService apiWebService = Global.PartnerWebServices.FirstOrDefault(x => x.PartnerId == partner.Id);
                if (apiWebService != null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new WebService(this.ControllerContext?.Request?.RequestUri?.AbsoluteUri, apiWebService));
                }
            }
            return this.CreateErrorResponse(HttpStatusCode.InternalServerError, "Partner or ApiWebService missing.", apiEndPoint);
        }


        /// <summary>
        /// Used for testing the FullMortgageRequest endpoint.
        /// </summary>
        /// <param name="partnerName">The name of the Partner this webservice is for.</param>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <returns cref="FullMortgageResponse">On success a FullMortgageResponse object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">FullMortgageResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid Partner Id)</response>
        [EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin, PerHour = 1000)] //enables Rate Limits
        //[Filters.PreProcessFilter] //does general PRE-processing for a web service call
        //[Filters.PostProcessFilter] //does general POST-processing for a web service call[Route("FullMortgageRequestTest")]
        [Route("FullMortgageRequest/Test")]
        [HttpGet] //used for testing
        [ResponseType(typeof(FullMortgageResponse<IQuoteSubmission<MortgageLoanQuote>>))]
        public virtual HttpResponseMessage FullMortgageRequestTest(string partnerName, string authToken)
        {
            string apiEndPoint = "FullMortgageRequest/Test";
            Partner partner = Global.Partners.FirstOrDefault(x => x.DefaultQuotingChannelType.ToString().Equals(partnerName, StringComparison.InvariantCultureIgnoreCase));
            if (partner == null || partner.ApiPartnerId == 0)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid Partner Name", apiEndPoint);

            ApiWebService apiWebService = Global.PartnerWebServices.FirstOrDefault(x => x.PartnerId == partner.Id && apiEndPoint.ToLower().StartsWith(x.EndPoint.ToLower()));
            if (apiWebService == null || apiWebService.Id == 0)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid Web Service / Endpoint", apiEndPoint);
            if (apiWebService.ApiStatusType == Types.Api.ApiStatusType.Inactive)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "In-active Web Service / Endpoint", apiEndPoint);

            AuthToken authTokenObject = (!string.IsNullOrEmpty(authToken)) ? new AuthToken(authToken) : null;
            HttpResponseMessage errorResponse = this.Authorize(apiWebService.WebServiceName, authTokenObject, partner.ApiPartnerId);
            if (errorResponse != null)
                return errorResponse;

            FullMortgageRequest request = this.CreateDummyRequest(partner.DefaultQuotingChannelType);
            
            errorResponse = this.CommonChecks(request, apiEndPoint);
            if (errorResponse != null)
                return errorResponse;

            this.CommonParams = new CommonParams(Request) { TimeoutInMill = 60000 };
            return this.FullMortgageRequest(partnerName, authToken, request);
        }

        /// <summary>
        /// Generate personalized mortgage quotes from active and enabled LoanTek Channel users/clients for a passed in mortgage request. 
        /// </summary>
        /// <remarks>
        /// LoanTek Clients are required to have an active and auto-quoting enabled LoanTek Channel in order to quote this request.
        /// </remarks>
        /// <param name="partnerName">The name of the Partner this webservice is for.</param>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <param name="request" cref="FullMortgageRequest">The mortgage request that contains the personal details about the mortgage.</param>
        /// <returns cref="FullMortgageResponse">On success a FullMortgageResponse object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">FullMortgageResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid Partner Id)</response>
        [EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin)] //enables Rate Limits
        //[Filters.PreProcessFilter] //does general PRE-processing for a web service call
        //[Filters.PostProcessFilter] //does general POST-processing for a web service call[Route("MortgageRequestLoadTest")]
        [Route("FullMortgageRequest")]
        [HttpPost]
        [ResponseType(typeof(FullMortgageResponse<IQuoteSubmission<MortgageLoanQuote>>))]
        public virtual HttpResponseMessage FullMortgageRequest(string partnerName, string authToken, FullMortgageRequest request)
        {
            DateTime startTime = DateTime.Now;

            string apiEndPoint = "FullMortgageRequest/POST";
            Partner partner = Global.Partners.FirstOrDefault(x => x.DefaultQuotingChannelType.ToString().Equals(partnerName, StringComparison.InvariantCultureIgnoreCase));
            if (partner == null|| partner.ApiPartnerId == 0)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid Partner Name", apiEndPoint);

            ApiWebService apiWebService = Global.PartnerWebServices.FirstOrDefault(x => x.PartnerId == partner.Id && apiEndPoint.ToLower().StartsWith(x.EndPoint.ToLower()));
            if (apiWebService == null || apiWebService.Id == 0)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid Web Service / Endpoint", apiEndPoint);
            if (apiWebService.ApiStatusType == Types.Api.ApiStatusType.Inactive)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "In-active Web Service / Endpoint", apiEndPoint);

            AuthToken authTokenObject = (!string.IsNullOrEmpty(authToken)) ? new AuthToken(authToken) : null;
            HttpResponseMessage errorResponse = this.Authorize(apiWebService.WebServiceName, authTokenObject, partner.ApiPartnerId);
            if (errorResponse != null)
                return errorResponse;

            errorResponse = this.CommonChecks(request, apiEndPoint);
            if (errorResponse != null)
                return errorResponse;

            this.CommonProcesses(Request);

            if (request.LoanRequest.QuotingChannel == QuotingChannelType.NotSpecified)
                request.LoanRequest.QuotingChannel = partner.DefaultQuotingChannelType;

            Service service = new Service();
            service.StartTime = startTime;
            service.UserId = CommonParams.UseOnlyThisUserId;
            service.ClientId = Master.Lists.Users.GetUserById(service.UserId)?.ClientId ?? 0;
            service.ApiWebServiceId = apiWebService.Id;
            service.Endpoint = apiEndPoint;
            service.Route = this.Request?.RequestUri?.AbsolutePath;
            service.ServiceName = apiWebService.WebServiceName;
            service.CallingAppType = Types.Api.AppType.Api_Partner.ToString();
            service.HttpStatusCode = HttpStatusCode.Accepted.ToString();
            service.CallingIpAddress = ClientInfo.GetIPAddress(this.Request);

            ProcessRequest processor = new ProcessRequest(new CommonConverter(), PreQualUsers.Instance, Cache.Instance);
            return this.ProcessMortgageRequest(processor, request, service);
        }

        #endregion

        #region Methods

        [ApiExplorerSettings(IgnoreApi = true)]
        public HttpResponseMessage ProcessMortgageRequest(ProcessRequest processor, FullMortgageRequest request, Service service)
        {
            try
            {
                Debug.WriteLine("NEW processMortgageRequest: " + request.ClientDefinedIdentifier);

                DateTime startTime = DateTime.Now;

                if(this.CommonParams == null)
                    this.CommonProcesses(Request);

                if(string.IsNullOrEmpty(request.LoanRequest.ClientDefinedIdentifier))
                    request.LoanRequest.ClientDefinedIdentifier = request.ClientDefinedIdentifier;

                CancellationTokenSource cancelToken = this.StartTimeoutTimer(CommonParams.TimeoutInMill);
                JsonSerializerSettings jsonSerializerSettings;

                FullMortgageResponse response = new FullMortgageResponse();

                if (!request.PostbackInChunks)
                {
                    // BStrong - 01/11/2017 - Using a new pattern for multi-threaded to try and fix blocked threading issue...
                    var doNotInsert = request.LoanRequest.QuotingChannel == QuotingChannelType.Bankrate;
                    var t = Task.Run(() => processor.Process(service, request.LoanRequest, JsonConvert.SerializeObject(request), cancelToken, CommonParams.UseOnlyThisUserId, CommonParams.DebugModeType, doNotInsert), cancelToken.Token);
                    t.Wait(cancelToken.Token);

                    response.Status = processor.Request.StatusType;
                    response.LoanTekDefinedIdentifier = (processor.Request.Id > 0) ? processor.Request.Id.ToString() : request.ClientDefinedIdentifier;
                    response.CachedId = processor.Request.CachedId;
                    response.Submissions = processor.Submissions.Select(x => x.LoanQuoteSubmission).ToList();
                    response.Message = processor.Request.Misc;

                    jsonSerializerSettings = new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = Common.Global.JsonSettings.ReferenceLoopHandling,
                        NullValueHandling = NullValueHandling.Include,
                        Formatting = Formatting.None,
                        DefaultValueHandling = DefaultValueHandling.Include
                    };
                    //if(!string.IsNullOrEmpty(request.CustomQuoteResponseJson))
                        jsonSerializerSettings.ContractResolver = new DynamicContractResolver(this.GetPropertiesToSerialize(request.CustomQuoteResponseJson));
                    jsonSerializerSettings.Converters.Add(new StringEnumConverter()); //convert enums from 'int' to 'string'
                }
                else
                {
#pragma warning disable 4014 //Allow the code to continue without an 'await'
                    processor.Process(service, request.LoanRequest, JsonConvert.SerializeObject(request), cancelToken, CommonParams.UseOnlyThisUserId, CommonParams.DebugModeType);
#pragma warning restore 4014
                    
                    response.Status = StatusType.Pending;
                    response.LoanTekDefinedIdentifier = (processor.Request.Id > 0) ? processor.Request.Id.ToString() : request.ClientDefinedIdentifier;
                    response.CachedId = null;
                    //response.Submissions = new List<IQuoteSubmission<Quote>>();
                    //processor.PreQualUsers.FilterByMortgageRequest(processor.PreQualUsers.GetQuotingUsersCopy(), request.LoanRequest)
                    //    .ForEach(x => response.Submissions.Add(new Converter.LoanQuoteSubmission() {QuotingUser = new Quoting.QuotingUser() {QuotingUsername = x.QuotingUsername}}));
                    response.Message = "posting results to:"+ request.PostbackUrl;
                    if (CommonParams.DebugModeType != Processing.DebugModeType.None)
                        response.Message += "|" + CommonParams.DebugModeType;
                    if (CommonParams.UseOnlyThisUserId > 0)
                        response.Message += "|" + CommonParams.UseOnlyThisUserId;
                    jsonSerializerSettings = Common.Global.JsonSettings;
                }

                response.ApiEndPoint = service?.Endpoint;
                response.ClientDefinedIdentifier = request.ClientDefinedIdentifier;
                response.PassThroughItems = request.PassThroughItems;
                response.ExecutionTimeInMillisec = Math.Round((DateTime.Now - startTime).TotalMilliseconds, 4);
                response.TimeStamp = DateAndTime.ConvertToUnixTime(DateTime.Now);

                //ActionContext.Request.Properties[Global.ARequestPropertyName] = response; //only needed if going to use a filter to save or log the response
                //if(string.IsNullOrEmpty(request.CustomQuoteResponseJson))
                //    return Request.CreateResponse(HttpStatusCode.OK, response);
                return Request.CreateResponse(HttpStatusCode.OK, response, new JsonMediaTypeFormatter() { SerializerSettings = jsonSerializerSettings });
            }
            catch (Exception ex)
            {
                Global.OutPrint(ex.Message, new SimpleLogger.LocationObject(this, "MortgageRequest/POST"), SimpleLogger.LogLevelType.CRITICAL);
                return this.CreateErrorResponse(HttpStatusCode.InternalServerError, "Exception:" + ex.Message, service?.Endpoint);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public FullMortgageRequest CreateDummyRequest(QuotingChannelType quotingChannelType)
        {
            FullMortgageRequest dummyRequest = new FullMortgageRequest();
            dummyRequest.PostbackInChunks = false;
            dummyRequest.PassThroughItems = new List<object>() {new {Item = "A Pass Through Item"}};
            dummyRequest.LoanRequest = new MortgageLoanRequest();
            ClassMappingUtilities.SetPropertiesForTarget(DummyData.GetRequest(), dummyRequest.LoanRequest);
            dummyRequest.ClientDefinedIdentifier = "LTP" + StringUtilities.UniqueId();
            dummyRequest.LoanRequest.QuotingChannel = quotingChannelType;
            return dummyRequest;
        }

        #endregion

    }
}
