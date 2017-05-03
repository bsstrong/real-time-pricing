using System;
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
using com.LoanTek.Biz.Api.Objects;
using com.LoanTek.Caching;
using com.LoanTek.Quoting.Mortgage.Common;
using com.LoanTek.Quoting.Mortgage.IData;
using com.LoanTek.Quoting.Mortgage.PreQualUsers;
using com.LoanTek.Types;
using LoanTek.LoggingObjects;
using LoanTek.Utilities;
using Newtonsoft.Json;
using Bankrate = com.LoanTek.Quoting.Mortgage.Bankrate;
using Partner = com.LoanTek.Biz.Pricing.Objects.Partner;
using PreQualConfig = com.LoanTek.Quoting.Mortgage.PreQualUsers.PreQualConfig;
using FullMortgageRequest = com.LoanTek.API.Pricing.Partners.Models.Mortgage.FullMortgageRequest;
using FullMortgageResponse = com.LoanTek.API.Pricing.Partners.Models.Mortgage.FullMortgageResponse<com.LoanTek.Quoting.Mortgage.Common.MortgageSubmission<com.LoanTek.Quoting.Mortgage.Bankrate.MortgageQuote>, com.LoanTek.Quoting.Mortgage.Bankrate.MortgageQuote>;

namespace com.LoanTek.API.Pricing.Partners.Controllers
{
    /// <summary>
    /// Webservice for Bankrate Quoting.  
    /// </summary>
    //[RoutePrefix("Pricing/Partner/{versionId}/Common/{authToken}/{partnerId}")] 'Pricing/Partner/{versionId}' is defined as a path on the server, so it is not needed here
    [RoutePrefix("Bankrate/{authToken}")]
    public class BankrateController : AApiController
    {
        public static string CacheHost;
        public static int? CachePort;
            
        private const string legacyAuthToken = "VWRsL3FrVllETEZFbnlocjZFbEJUcHRvOU1NRG11eGx3N0NZeW84YjRZWE1vc3VOY0cwdkYxMXZKWkRZU0tHc2c1VUd2MG9nMmI0QQ2";
        //private const long ratePerSec = 100;
        //private const long ratePerMin = 6000;
        public const string RoutePrefix = "Bankrate/{authToken}";
        public static Partner Partner;
        public static ApiWebService ApiWebService;
        public static string GetAuthToken() => legacyAuthToken;
        private static JsonSerializerSettings jsonSerializerSettings;

        private static readonly object syncInit = new object();
        //Static constructor is used to initialize any static data or to perform a particular action that needs to be preformed once only
        private static void init()
        {
            if (Partner == null)
            {
                lock (syncInit)
                {
                    if (Partner == null)
                    {
                        Partner = Global.Partners?.FirstOrDefault(x => x.Name == "Bankrate");
                        ApiWebService = Global.PartnerWebServices?.FirstOrDefault(x => x.PartnerId == Partner?.Id);
                        if (Partner == null || ApiWebService == null)
                            throw new Exception("Partner and/or ApiWebService objects are null @ static BankrateController()");

                        //preload a select number of pricing engines...
                        //Task.Run(() => PricingEngineListNoLock2.Instance.InitPricingEngines(Bankrate.PreQualUsers.Instance.Get().Select(x => x.UserId).Distinct().Take(5).ToList()));

                        Bankrate.PreQualUsers.Config = new PreQualConfig()
                        {
                            UpdateDataCheckInterval = TimeSpan.FromMinutes(2)
                        };
                        // ReSharper disable once UnusedVariable
                        var i = Bankrate.PreQualUsers.Instance; //init prequal

                        Bankrate.Cache.Config = new RegionCacheConfig();
                        Bankrate.Cache.Config.CacheHandleConfigs.Add(new CacheHandleConfig(CacheSystemType.Dictionary, CacheHandleType.InProcess.ToString(), ExpirationModeType.Absolute, TimeSpan.FromMinutes(30)));
                        Bankrate.Cache.Config.CacheHandleConfigs.Add(new CacheHandleConfig.RedisCacheHandleConfig(CacheSystemType.Redis, CacheHandleType.Shared.ToString(), ExpirationModeType.Absolute, TimeSpan.FromMinutes(60))
                        {
                            ConnectionTimeout = 10000,
                            Host = CacheHost ?? "cachelb.loantek.com",
                            Port = CachePort ?? 6379
                        });
                        // ReSharper disable once UnusedVariable
                        var c = Bankrate.Cache.Instance; //init the cache

                        jsonSerializerSettings = new JsonSerializerSettings
                        {
                            ReferenceLoopHandling = Common.Global.JsonSettings.ReferenceLoopHandling,
                            NullValueHandling = NullValueHandling.Ignore,
                            Formatting = Formatting.None,
                            DefaultValueHandling = DefaultValueHandling.Include
                        };
                        //jsonSerializerSettings.Converters.Add(new StringEnumConverter()); //convert enums from 'int' to 'string'
                    }
                }
            }
        }

        public BankrateController()
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
        //[IPAddressValidation]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("PreQualList")]
        [HttpGet]
        [HttpPost]
        public HttpResponseMessage GetPreQualList(string authToken, MortgageRequest request)
        {
            string apiEndPoint = "PreQualList";
            if (authToken != legacyAuthToken)
            {
                AuthToken authTokenObject = (!string.IsNullOrEmpty(authToken)) ? new AuthToken(authToken) : null;
                HttpResponseMessage errorResponse = this.Authorize(apiEndPoint, authTokenObject, Partner?.ApiPartnerId ?? 0);
                if (errorResponse != null)
                    return errorResponse;
            }
            //var s = JsonConvert.SerializeObject(request?.Form == null ? Bankrate.PreQualUsers.Instance.Get() : Bankrate.PreQualUsers.Instance.Get(request), Global.JsonSettingsMin);
            return Request.CreateResponse(HttpStatusCode.OK, request?.Form == null ? Bankrate.PreQualUsers.Instance.Get() : Bankrate.PreQualUsers.Instance.Get(request), new JsonMediaTypeFormatter() {SerializerSettings = Global.JsonSettingsMin} );
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
            return Request.CreateResponse(HttpStatusCode.OK, new CacheInstance(this.ControllerContext?.Request?.RequestUri?.AbsoluteUri, Bankrate.Cache.Instance));

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

        #region Mortgage Requests

        /// <summary>
        /// Used for testing the FullMortgageRequest endpoint.
        /// </summary>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <returns cref="FullMortgageResponse">On success a FullMortgageResponse object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">FullMortgageResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid Partner Id)</response>
        //[EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin, PerHour = 10000)] //enables Rate Limits
        [Route("FullMortgageRequest/Test")]
        [HttpGet] //used for testing
        [ResponseType(typeof(FullMortgageResponse))]
        public HttpResponseMessage FullMortgageRequestTest(string authToken)
        {
            this.EndPoint = "FullMortgageRequest/Test";
            Partner partner = Partner;
            if (partner == null || partner.ApiPartnerId == 0)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid Partner", this.EndPoint);

            FullMortgageRequest request = DummyData.CreateDummyRequest(partner.DefaultQuotingChannelType);
            request.UserId = this.CommonProcesses(this.Request)?.UseOnlyThisUserId ?? 0;
            if(request.UserId == 0)
                throw new Exception("UserId required for test. Use query param ?UseOnlyThisUserId=userId");

            this.CommonParams.TimeoutInMill = 60000;
            return this.FullMortgageRequest(authToken, request);
        }

        /// <summary>
        /// Generate personalized mortgage quotes from active and enabled Bankrate Channel users for a passed in mortgage request. 
        /// </summary>
        /// <remarks>
        /// LoanTek Clients are required to have an active and auto-quoting enabled Bankrate Channel in order to quote this request.
        /// </remarks>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <param name="request" cref="Models.Common.FullMortgageRequest">The mortgage request that contains the personal details about the mortgage.</param>
        /// <returns cref="FullMortgageResponse">On success a FullMortgageResponse object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">FullMortgageResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid or Unauthorized Authentication Token</response>
        /// <response code="403">Access not allowed (Invalid Partner Id)</response>
        [Route("FullMortgageRequest")]
        [HttpPost]
        [ResponseType(typeof(FullMortgageResponse))]
        public HttpResponseMessage FullMortgageRequest(string authToken, FullMortgageRequest request)
        {
            var startTime = DateTime.Now;
            this.EndPoint = EndPoint ?? ApiWebService.EndPoint +"/POST";
            Partner partner = Partner;
            if (partner?.ApiPartnerId == 0)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid Partner", this.EndPoint);

            if (request.UserId == 0 && this.CommonProcesses(this.Request)?.UseOnlyThisUserId > 0)
                request.UserId = CommonParams.UseOnlyThisUserId;
            if (request.UserId == 0)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Required UserId is missing or zero.", this.EndPoint);

            HttpResponseMessage errorResponse;
            if (authToken != legacyAuthToken)
            {
                AuthToken authTokenObject = (!string.IsNullOrEmpty(authToken)) ? new AuthToken(authToken) : null;
                errorResponse = this.Authorize(this.EndPoint, authTokenObject, Partner?.ApiPartnerId ?? 0);
                if (errorResponse != null)
                    return errorResponse;
            }

            errorResponse = this.CommonChecks(request, this.EndPoint);
            if (errorResponse != null)
                return errorResponse;

            request.Form.QuotingChannelType = QuotingChannelType.Bankrate;

            #region Create 'Service'
            Service service = new Service();
            service.StartTime = startTime;
            service.ClientId = 0;
            service.UserId = request.UserId;
            service.RequestId = request.ClientDefinedId;
            service.ApiWebServiceId = ApiWebService.Id;
            service.Endpoint = ApiWebService.EndPoint;
            service.Route = ApiWebService.Domain + "/authkey/" + this.EndPoint;
            service.ServiceName = ApiWebService.WebServiceName;
            service.CallingAppType = Types.Api.AppType.Api;
            service.HttpStatusCodeType = HttpStatusCode.Accepted;
            service.CallingIpAddress = ClientInfo.GetIPAddress(this.Request);
            #endregion

            Bankrate.ProcessRequest processor = new Bankrate.ProcessRequest(new Bankrate.Converter("BR-", "BQ-"), Bankrate.PreQualUsers.Instance, Bankrate.Cache.Instance);
            return this.processMortgageRequest(processor, request, service);
        }

        #endregion

        [ApiExplorerSettings(IgnoreApi = true)]
        private HttpResponseMessage processMortgageRequest(Bankrate.ProcessRequest processor, FullMortgageRequest request, Service service)
        {
            try
            {
                //Debug.WriteLine("NEW Bankrate ProcessMortgageRequest: " + request.ClientDefinedId);
                //Debug.WriteLine(JsonConvert.SerializeObject(request));

                Stopwatch sw = new Stopwatch();
                sw.Start();

                this.CommonProcesses(Request);

                CancellationTokenSource cancelToken = this.StartTimeoutTimer(CommonParams.TimeoutInMill);
     
                //await Task.Run(() =>
                //{
                    processor.Process(request, JsonConvert.SerializeObject(request), cancelToken, this.DoNotInsert, CommonParams.DebugModeType, service);
                //}, cancelToken.Token);

                FullMortgageResponse response = new FullMortgageResponse();
                response.StatusType = processor.Request.StatusType;
                if(response.StatusType == Processing.StatusType.Complete || response.StatusType == Processing.StatusType.Save)
                    response.StatusType = Processing.StatusType.Success;
                response.LoanTekDefinedId = processor.Request?.RequestId;
                response.CachedId = processor.Request.CachedId;
                response.Submissions = processor.SubmissionsList?.Where(x => x != null).Select(x => x.GetLoanSubmission<MortgageSubmission<Bankrate.MortgageQuote>>()).ToList();
                response.Message = string.Join(";", processor.Request.Misc);

                if (request.PassThroughItems?.FirstOrDefault() as string == "ShowTimeObjects")
                {
                    response.RequestTimeObjects = processor.Request.Times?.ToList();
                    response.SubmissionTimeObjects = processor.SubmissionsList?.FirstOrDefault()?.Times?.ToList();
                }

                //jsonSerializerSettings.ContractResolver = new DynamicContractResolver(this.GetPropertiesToSerialize(request.CustomQuoteResponseJson));
                //jsonSerializerSettings.Converters.Add(new StringEnumConverter()); //convert enums from 'int' to 'string'

                response.ApiEndPoint = Environment.MachineName +"|"+ service.Endpoint;
                response.ClientDefinedId = request.ClientDefinedId;
                response.PassThroughItems = request.PassThroughItems;
                response.ExecutionTimeInMillisec = Math.Round((double)sw.ElapsedMilliseconds, 4);
                response.TimeStamp = DateAndTime.ConvertToUnixTime(DateTime.Now);

                if(Debugger.IsAttached)
                    outPrint(processor);

                return Request.CreateResponse(HttpStatusCode.OK, response, new JsonMediaTypeFormatter() { SerializerSettings = jsonSerializerSettings });
            }
            catch (Exception ex)
            {
                string msg = (ex.InnerException?.Message ?? ex.Message) +"\nStack:"+ ex.StackTrace;
                Common.Global.OutPrint(msg + "\nStack:"+ ex.StackTrace, new SimpleLogger.LocationObject(this, "processMortgageRequest"), SimpleLogger.LogLevelType.CRITICAL);
                return this.CreateErrorResponse(HttpStatusCode.InternalServerError, "Exception:" + msg, service?.Endpoint);
            }
        }

        private void outPrint(Bankrate.ProcessRequest processor)
        {
            if (processor == null)
                return;
            Debug.WriteLine("\n"+ processor.Request.RequestId +" for "+ processor.Request.UserId + " "+ processor.Request.StatusType + " TotalTime:" + (processor.Request.EndTime.GetValueOrDefault() - processor.Request.StartTime).TotalSeconds + " -quote count:" + processor.Request.QuotesSentCount + " -msg:"+ string.Join("|", processor.Request.Misc));
            var times = processor.Request.Times.ToList();
            foreach (var secPerRequest in times)
            {
                Debug.WriteLine("" + secPerRequest.StatusType + " milli:" + secPerRequest.ElapsedMilliseconds);
            }
            times = processor.SubmissionsList?.FirstOrDefault()?.Times.ToList();
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
