using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Description;
using com.LoanTek.API.Common.Filters;
using com.LoanTek.API.Common.Models;
using com.LoanTek.API.Instances;
using com.LoanTek.API.Pricing.Clients.Models;
using com.LoanTek.API.Pricing.Partners.Models;
using com.LoanTek.Biz.Api.Objects;
using com.LoanTek.Caching;
using com.LoanTek.Forms.Mortgage;
using com.LoanTek.Quoting.Mortgage;
using com.LoanTek.Quoting.Mortgage.Common;
using com.LoanTek.Quoting.Mortgage.IData;
using com.LoanTek.Quoting.Mortgage.PreQualUsers;
using com.LoanTek.Types;
using LoanTek.LoggingObjects;
using LoanTek.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using WebApiThrottle;
using AApiController = com.LoanTek.API.Pricing.Clients.Models.AApiController;
using Cache = com.LoanTek.Quoting.Mortgage.Common.Cache;
using Partner = com.LoanTek.Biz.Pricing.Objects.Partner;
using PreQualConfig = com.LoanTek.Quoting.Mortgage.PreQualUsers.PreQualConfig;
using FullMortgageRequest = com.LoanTek.API.Pricing.Partners.Models.Mortgage.FullMortgageRequest;
using FullMortgageResponse = com.LoanTek.API.Pricing.Partners.Models.Mortgage.FullMortgageResponse<com.LoanTek.Quoting.Mortgage.Common.MortgageSubmission<com.LoanTek.Quoting.Mortgage.Common.MortgageQuote>, com.LoanTek.Quoting.Mortgage.Common.MortgageQuote>;
#pragma warning disable 1591

namespace com.LoanTek.API.Pricing.Clients.Controllers
{
    /// <summary>
    /// Webservice for New MortgageLoan Quoting.  
    /// </summary>
    [RoutePrefix("MortgageLoan/Price/{authToken}")]
    [ApiExplorerSettings(IgnoreApi = true)] //only for interal use for now...
    public class MortgageLoanPricerController : AApiController, IApiController
    {
        private const int loanTekPartnerId = 1;
        private const string webServiceName = "MortgageLoanPrice";

        public virtual long RatePerSec => ratePerSec;
        public virtual long RatePerMin => ratePerMin;
        public virtual Partner Partner { get { return partner; } set { partner = value; } }
        public virtual ApiWebService ApiWebService { get { return apiWebService; } set { apiWebService = value; } }
        public virtual JsonSerializerSettings JsonSerializerSettings { get { return jsonSerializerSettings; } set { jsonSerializerSettings = value; } }

        private const long ratePerSec = 10;   
        private const long ratePerMin = 600;
        private static Partner partner;
        private static ApiWebService apiWebService;
        private static JsonSerializerSettings jsonSerializerSettings;

        private static readonly object syncInit = new object();
        //Static constructor is used to initialize any static data or to perform a particular action that needs to be preformed once only
        private static void init()
        {
            if (partner == null)
            {
                lock (syncInit)
                {
                    if (partner == null)
                    {
                        partner = Global.Partners.FirstOrDefault(x => x.Id == loanTekPartnerId);
                        apiWebService = Global.ClientWebServices?.FirstOrDefault(x => x.PartnerId == partner?.Id && x.WebServiceName == webServiceName);
                        if (partner == null || apiWebService == null)   
                            throw new NoNullAllowedException("Fatal Error @ Clients."+ typeof(MortgageLoanPricerController).GetCustomAttribute<RoutePrefixAttribute>() +" Partner and/or ApiWebService data is missing.");

                        PreQualUsers.Config = new PreQualConfig()
                        {
                            UpdateDataCheckInterval = TimeSpan.FromMinutes(2)
                        };
                        // ReSharper disable once UnusedVariable
                        var i = PreQualUsers.Instance; //init prequal

                        Cache.Config = new RegionCacheConfig();
                        Cache.Config.CacheHandleConfigs.Add(new CacheHandleConfig(CacheSystemType.Dictionary, CacheHandleType.InProcess.ToString(), ExpirationModeType.Absolute, TimeSpan.FromHours(1)));
                        //Cache.Config.CacheHandleConfigs.Add(new CacheHandleConfig.RedisCacheHandleConfig(CacheSystemType.Redis, CacheHandleType.Shared.ToString(), ExpirationModeType.Absolute, TimeSpan.FromMinutes(60))
                        //{
                        //    ConnectionTimeout = 10000,
                        //    Host = CacheHost ?? "cachelb.loantek.com",
                        //    Port = CachePort ?? 6379
                        //});
                        // ReSharper disable once UnusedVariable
                        var c = Cache.Instance; //init the cache

                        jsonSerializerSettings = new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = Common.Global.JsonSettings.ReferenceLoopHandling,
                            NullValueHandling = NullValueHandling.Include,
                            Formatting = Formatting.None,
                            DefaultValueHandling = DefaultValueHandling.Include
                        };
                        jsonSerializerSettings.Converters.Add(new StringEnumConverter()); //convert enums from 'int' to 'string'
                    }
                }
            }
        }

        public MortgageLoanPricerController()
        {
            init();
        }

        #region PreQual

        /// <summary>
        /// Used to query this particular web service for a list of Pre-Qualified / Active quoters for this Channel. 
        /// </summary>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <param name="request" cref="FullMortgageRequest">The mortgage request that contains the personal details about the mortgage.</param>
        /// <returns cref="QuotingUser">List of QuotingUser objects.</returns>
        [IPAddressValidation]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("PreQualList")]
        [HttpGet]
        public HttpResponseMessage GetPreQualList(string authToken, IMortgageRequest<IMortgageForm> request)
        {
            //string apiEndPoint = "PreQualList";
            return Request.CreateResponse(HttpStatusCode.OK, request?.Form == null ? PreQualUsers.Instance.Get() : PreQualUsers.Instance.Get(request));
        }

        #endregion

        /// <summary>
        /// Get cache instance data and clear the cache.
        /// </summary>
        /// <param name="clear">Set to True if you want to clear the cache.</param>
        /// <param name="region">Set to the UserId if you want to clear only the cache for a single user.</param>
        /// <returns></returns>
        [IPAddressValidation]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("Cache")]
        [HttpGet]
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

        #region Mortgage Requests

        /// <summary>
        /// Used to query information about this particular web service. 
        /// </summary>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <returns cref="WebService">WebService object containing details about this web service.</returns>
        [Route("Info")]
        [HttpGet]
        public HttpResponseMessage Info(string authToken)
        {
            //string apiEndPoint = "FullMortgageRequest/Info";
            var obj = new WebService(this.ControllerContext?.Request?.RequestUri?.AbsoluteUri, ApiWebService);
            return Request.CreateResponse(HttpStatusCode.OK, obj);
        }

        /// <summary>
        /// Used for website's LoanPricer.
        /// </summary>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <param name="request" cref="Partners.Models.Common.FullMortgageRequest">The mortgage request that contains the personal details about the mortgage.</param>
        /// <returns cref="FullMortgageResponse">On success a FullMortgageResponse object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">FullMortgageResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid Partner Id)</response>
        [EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin, PerHour = 1000)] //enables Rate Limits
        [Route("LoanPricer")]
        [HttpPost]
        public HttpResponseMessage MortgageRequestLoanPricer(string authToken, FullMortgageRequest request)
        {   
            this.Stopwatch.Start();
            this.AppType = Types.Api.AppType.LoanPricer;
            const string endPoint = "LoanPricer";

            HttpResponseMessage errorResponse = this.SetApiAndEndPoint(ApiWebService, endPoint);
            if (errorResponse != null)
                return errorResponse;

            if (request.UserId == 0 && this.CommonProcesses(this.Request)?.UseOnlyThisUserId > 0)
                request.UserId = CommonParams.UseOnlyThisUserId;
            if (request.UserId == 0)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Required UserId is missing or zero.", endPoint);

            errorResponse = this.CommonChecks(request, endPoint);
            if (errorResponse != null)
                return errorResponse;

            errorResponse = this.Authorize(ApiWebService.WebServiceName, authToken, request.UserId);
            if (errorResponse != null)
                return errorResponse;

            Service service = this.CreateService(request, ApiWebService, endPoint);

            ProcessRequest processor = new ProcessRequest(new Converter("LTLPR-", "LTLPQ-"), PreQualUsers.Instance, Cache.Instance);
            return this.ProcessMortgageRequest(processor, request, service);
        }

        /// <summary>
        /// Used for testing the FullMortgageRequest endpoint.
        /// </summary>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <returns cref="FullMortgageResponse">On success a FullMortgageResponse object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">FullMortgageResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid Partner Id)</response>
        [EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin, PerHour = 1000)] //enables Rate Limits
        [Route("FullMortgageRequest/Test")]
        [HttpGet] //used for testing
        //[ResponseType(typeof(MortgageResponse<Common.q>>))]
        public HttpResponseMessage MortgageRequestTest(string authToken)
        {
            this.Stopwatch.Start();
            this.AppType = Types.Api.AppType.Api;
            const string endPoint = "FullMortgageRequest/Test";

            HttpResponseMessage errorResponse = this.SetApiAndEndPoint(ApiWebService, endPoint);
            if (errorResponse != null)
                return errorResponse;

            var userId = this.CommonProcesses(this.Request)?.UseOnlyThisUserId ?? 0;
            if (userId == 0)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "UserId required for test. Use query param ?UseOnlyThisUserId=userId", endPoint);

            errorResponse = this.Authorize(ApiWebService.WebServiceName, this.AuthToken, userId);
            if (errorResponse != null)
                return errorResponse;

            FullMortgageRequest request = DummyData.CreateDummyRequest(Partner.DefaultQuotingChannelType);
            request.UserId = userId;
            
            errorResponse = this.CommonChecks(request, endPoint);
            if (errorResponse != null)
                return errorResponse;

            Service service = this.CreateService(request, ApiWebService, endPoint);

            this.CommonParams.TimeoutInMill = 60000;

            ProcessRequest processor = new ProcessRequest(new Converter("TEST-", "TEST-"), PreQualUsers.Instance, Cache.Instance);
            return this.ProcessMortgageRequest(processor, request, service);
        }

        /// <summary>
        /// Generate personalized mortgage quotes from active and enabled LoanTek users for a passed in mortgage request. 
        /// </summary>
        /// <remarks>
        /// LoanTek Clients are required to have an active and auto-quoting enabled License in order to quote this request.
        /// </remarks>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <param name="request" cref="Partners.Models.Common.FullMortgageRequest">The mortgage request that contains the personal details about the mortgage.</param>
        /// <returns cref="FullMortgageResponse">On success a FullMortgageResponse object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">FullMortgageResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid Partner Id)</response>
        [Route("FullMortgageRequest")]
        [HttpPost]
        //[ResponseType(typeof(FullMortgageResponse<IQuoteSubmission<Converter.DefaultQuote>>))]
        public HttpResponseMessage FullMortgageRequest(string authToken, FullMortgageRequest request)
        {
            this.Stopwatch.Start();
            this.AppType = Types.Api.AppType.Api;
            const string endPoint = "FullMortgageRequest";

            HttpResponseMessage errorResponse = this.SetApiAndEndPoint(ApiWebService, endPoint);
            if (errorResponse != null)
                return errorResponse;

            if (request.UserId == 0 && this.CommonProcesses(this.Request)?.UseOnlyThisUserId > 0)
                request.UserId = CommonParams.UseOnlyThisUserId;
            if (request.UserId == 0)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Required UserId is missing or zero.", endPoint);

            errorResponse = this.CommonChecks(request, endPoint);
            if (errorResponse != null)
                return errorResponse;

            errorResponse = this.Authorize(ApiWebService.WebServiceName, authToken, request.UserId);
            if (errorResponse != null)
                return errorResponse;

            Service service = this.CreateService(request, ApiWebService, endPoint);
 
            ProcessRequest processor = new ProcessRequest(new Converter("LTR-", "LTQ-"), PreQualUsers.Instance, Cache.Instance);
            return this.ProcessMortgageRequest(processor, request, service);
        }

        #endregion

        [ApiExplorerSettings(IgnoreApi = true)]
        protected HttpResponseMessage ProcessMortgageRequest(ProcessRequest processor, FullMortgageRequest request, Service service)
        {
            try
            {
                //Debug.WriteLine("NEW MortgageLoanNew ProcessMortgageRequest: " + request.ClientDefinedId);
                //Debug.WriteLine(JsonConvert.SerializeObject(request));

                if (request.Form.IsValid() != null)
                    return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Loan Request Form error(s):" + string.Join(";", request.Form.IsValid()), this.EndPoint?.EndPoint);

                this.CommonProcesses(Request);

                CancellationTokenSource cancelToken = this.StartTimeoutTimer(Debugger.IsAttached ? 30000 : CommonParams.TimeoutInMill);
     
                processor.Process(request, JsonConvert.SerializeObject(request), cancelToken, this.DoNotInsert, CommonParams.DebugModeType, service);
        
                FullMortgageResponse response = new FullMortgageResponse();
                response.StatusType = processor.Request.StatusType;
                if(response.StatusType == Processing.StatusType.Complete || response.StatusType == Processing.StatusType.Save)
                    response.StatusType = Processing.StatusType.Success;
                response.LoanTekDefinedId = processor.Request?.RequestId;
                response.CachedId = processor.Request.CachedId;
                response.Submissions = processor.SubmissionsList?.Where(x => x != null).Select(x => x.GetLoanSubmission<MortgageSubmission<MortgageQuote>>()).ToList();

                #region TODO - temp to bypass QuoteTypesToReturn error
                //if (request.Form.QuoteTypesToReturn != null)
                //{
                //    response.Submissions?.ForEach(z => z.Quotes?.RemoveAll(x => request.Form.QuoteTypesToReturn.All(y => y != x.QuoteTypeType)));
                //}
                #endregion

                response.Message = string.Join(";", processor.Request.Misc);

                #region RequestTimeObjects
                if (request.PassThroughItems?.FirstOrDefault() as string == "ShowTimeObjects")
                {
                    response.RequestTimeObjects = processor.Request.Times?.ToList();
                    response.SubmissionTimeObjects = processor.SubmissionsList?.FirstOrDefault()?.Times?.ToList();
                }
                #endregion

                #region CustomQuoteResponseJson 
                JsonSerializerSettings customSettings;
                if (!string.IsNullOrEmpty(request.CustomQuoteResponseJson))
                {
                    customSettings = new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = Common.Global.JsonSettings.ReferenceLoopHandling,
                        NullValueHandling = NullValueHandling.Ignore,
                        Formatting = Formatting.None,
                        DefaultValueHandling = DefaultValueHandling.Include
                    };
                    customSettings.ContractResolver = new DynamicContractResolver<MortgageQuote, IMortgageQuote>(this.GetPropertiesToSerialize(request.CustomQuoteResponseJson));
                    customSettings.Converters.Add(new StringEnumConverter()); //convert enums from 'int' to 'string'
                }
                else
                    customSettings = this.JsonSerializerSettings;
                #endregion

                response.ApiEndPoint = service.Endpoint;
                response.ClientDefinedId = request.ClientDefinedId;
                response.PassThroughItems = request.PassThroughItems;
                response.ExecutionTimeInMillisec = Math.Round((double) this.Stopwatch.ElapsedMilliseconds, 4);
                response.TimeStamp = DateAndTime.ConvertToUnixTime(DateTime.Now);

                if(Debugger.IsAttached)
                    outPrint(processor);

                return Request.CreateResponse(HttpStatusCode.OK, response, new JsonMediaTypeFormatter() { SerializerSettings = customSettings });
            }
            catch (Exception ex)
            {
                Common.Global.OutPrint(ex.Message, new SimpleLogger.LocationObject(this, "processMortgageRequest"), SimpleLogger.LogLevelType.CRITICAL);
                return this.CreateErrorResponse(HttpStatusCode.InternalServerError, "Exception:" + ex.Message, service?.Endpoint);
            }
        }

        private void outPrint(ProcessRequest processor)
        {
            if (processor == null)
                return;
            Debug.WriteLine("\n"+ processor.Request.RequestId +" for "+ processor.Request.UserId + " "+ processor.Request.StatusType + " TotalTime:" + (processor.Request.EndTime.GetValueOrDefault() - processor.Request.StartTime).TotalSeconds + " -quote count:" + processor.Request.QuotesSentCount + " -msg:"+ string.Join("|", processor.Request.Misc));
            var times = processor.Request.Times?.ToList();
            if (times != null)
            {
                foreach (var secPerRequest in times)
                {
                    Debug.WriteLine("" + secPerRequest.StatusType + " milli:" + secPerRequest.ElapsedMilliseconds);
                }
            }
            times = processor.SubmissionsList?.FirstOrDefault()?.Times?.ToList();
            if (times != null)
            {
                foreach (var secPerRequest in times)
                {
                    Debug.WriteLine(" -submission:" + processor.SubmissionsList?.FirstOrDefault()?.UserId + " - " + secPerRequest.StatusType + " milli:" + secPerRequest.ElapsedMilliseconds);
                }
            }
        }

        
    }
}
