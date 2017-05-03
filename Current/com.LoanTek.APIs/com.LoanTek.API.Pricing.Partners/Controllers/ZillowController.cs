using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using com.LoanTek.API.Common.Filters;
using com.LoanTek.API.Common.Models;
using com.LoanTek.API.Instances;
using com.LoanTek.API.Pricing.Partners.Models;
using com.LoanTek.API.Pricing.Partners.Models.Common;
using com.LoanTek.Biz.Api.Objects;
using com.LoanTek.Caching;
using com.LoanTek.Master.Data.LinqDataContexts;
using com.LoanTek.Quoting;
using com.LoanTek.Quoting.Zillow;
using com.LoanTek.Types;
using LoanTek.LoggingObjects;
using LoanTek.Pricing.Zillow;
using LoanTek.Utilities;
using Newtonsoft.Json;
using WebApiThrottle;
using BusinessObjects = LoanTek.Pricing.BusinessObjects;
using Partner = com.LoanTek.Biz.Pricing.Objects.Partner;

namespace com.LoanTek.API.Pricing.Partners.Controllers
{
    /// <summary>
    /// Webservice for Zillow Quoting.
    /// </summary>
    /// <remarks>
    /// Please refer to Global.asax for Cache and PreQual instance inits
    /// </remarks>
    /// 
    //[RoutePrefix("https://zillow-pricing-api.LoanTek.com/{versionId}/Zillow/")] '/{versionId}' is defined as a path on the server, so it is not needed here
    [RoutePrefix("Zillow/{authToken}")]
    public class ZillowController : AApiController
    {
        private const string legacyAuthToken = "NVV1a3VWR0ZIbEdmbDc0YVJ2MEZoa08zSGRvV2JJSC9tbUlTeGN6dFpVNUI5Z0IyNjZtK1FFREI1YXpxTE84d0FBPT01";
        public const string RoutePrefix = "Zillow/{authToken}";
        public static Partner Partner;
        public static ApiWebService ApiWebService;

        private const long ratePerSec = 200;
        private const long ratePerMin = 12000;

        private static DateTime preQualLastUpdate;

        //Static constructor is used to initialize any static data or to perform a particular action that needs to be preformed once only
        static ZillowController()
        {
            Partner = Global.Partners.FirstOrDefault(x => x.Name == "Zillow");
            ApiWebService = Global.PartnerWebServices.FirstOrDefault(x => x.PartnerId == Partner?.Id);
            if (Partner == null || ApiWebService == null)
                throw new Exception("Partner and/or ApiWebService objects are null @ static ZillowController()");

            PreQualUsers.Config = new PreQualConfig()
            {
                QuoteSysDataContextConnStr = new QuoteSystemsDataContext().Connection.ConnectionString,
                LoanTekDataContextConnStr = new LoanTekDataContext().Connection.ConnectionString,
                UpdateDataCheckInterval = TimeSpan.FromMinutes(2),
                ApplicationServerName = Environment.MachineName
            };

            Cache.Config = new RegionCacheConfig();
            Cache.Config.CacheHandleConfigs.Add(new CacheHandleConfig(CacheSystemType.Dictionary, CacheHandleType.InProcess.ToString(), ExpirationModeType.Absolute, TimeSpan.FromMinutes(120)));
            Cache.Config.CacheHandleConfigs.Add(new CacheHandleConfig.RedisCacheHandleConfig(CacheSystemType.Redis, CacheHandleType.Shared.ToString(), ExpirationModeType.Absolute, TimeSpan.FromMinutes(240))
            {
                ConnectionTimeout = 10000,
                Host = "10.83.95.37",
                Port = 6379,
                Password = "MANTLE7"
            }); 
            
            //Task.Run(() => PricingEngineList.Instance.InitPricingEngines(Zillow.PreQualUsers.Instance.OptimizedList.Select(x => x.UserId).ToList()));
        }

        #region PreQual

        /// <summary>
        /// Used to query this particular web service for a list of Pre-Qualified / Active quoters for this Channel. 
        /// </summary>
        /// <param name="authToken">Unique security token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <param name="request" cref="FullMortgageRequest">The mortgage request that contains the personal details about the mortgage.</param>
        /// <returns cref="Api">Api object containing details about this web service.</returns>
        [Route("PreQualList")]
        [HttpGet]
        [HttpPost]
        public HttpResponseMessage GetPreQualList(string authToken, zillowLoanRequestNotification request)
        {
            string apiEndPoint = "PreQualList";
            if (authToken != legacyAuthToken)
            {
                AuthToken authTokenObject = (!string.IsNullOrEmpty(authToken)) ? new AuthToken(authToken) : null;
                HttpResponseMessage errorResponse = this.Authorize(apiEndPoint, authTokenObject, Partner?.ApiPartnerId ?? 0);
                if (errorResponse != null)
                    return errorResponse;
            }
            var mortgageLoanRequest = ZillowMortgage.GetMortgageLoanRequest(request);

            return Request.CreateResponse(HttpStatusCode.OK, mortgageLoanRequest?.ZipCode == null ? PreQualUsers.Instance.OptimizedList : PreQualUsers.Instance.FilterByMortgageRequest(PreQualUsers.Instance.OptimizedList, mortgageLoanRequest));
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
            return Request.CreateResponse(HttpStatusCode.OK, new CacheInstance(this.ControllerContext?.Request?.RequestUri?.AbsoluteUri, Cache.Instance));
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
        [Route("MortgageRequest/Info")]
        [HttpGet]
        public HttpResponseMessage FullMortgageRequestInfo(string authToken)
        {
            //string apiEndPoint = "FullMortgageRequest/Info";
            return Request.CreateResponse(HttpStatusCode.OK, new WebService(this.ControllerContext?.Request?.RequestUri?.AbsoluteUri, ApiWebService));
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
        [EnableThrottling(PerSecond = 10, PerMinute = 1000, PerHour = 10000)] //enables Rate Limits
        [Route("MortgageRequest/Test")]
        [HttpGet] //used for testing
        public HttpResponseMessage MortgageRequestTest(string authToken)
        {
            string apiEndPoint = "MortgageRequest/Test";
    
            if (authToken != legacyAuthToken)
            {
                AuthToken authTokenObject = (!string.IsNullOrEmpty(authToken)) ? new AuthToken(authToken) : null;
                HttpResponseMessage errorResponse = this.Authorize(apiEndPoint, authTokenObject, Partner?.ApiPartnerId ?? 0);
                if (errorResponse != null)
                    return errorResponse;
            }

            string request = this.CreateDummyRequest(this.Request.GetQueryNameValuePairs().ToList());
            this.CommonParams = new CommonParams(Request) { TimeoutInMill = 60000 };
            return this.MortgageRequest(authToken, JsonConvert.DeserializeObject<zillowLoanRequestNotification>(request));
        }

        [EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin)] //enables Rate Limits
        //We do not use the Pre and Post filters for Zillow requests!!!
        //[Filters.PreProcessFilter] //does general PRE-processing for a web service call
        //[Filters.PostProcessFilter] //does general POST-processing for a web service call
        [Route("MortgageRequest")]
        [HttpPost]
        public HttpResponseMessage MortgageRequest(string authToken, zillowLoanRequestNotification request)
        {
            string apiEndPoint = "MortgageRequest/POST";
            try
            {
                DateTime startTime = DateTime.Now;

                if (string.IsNullOrEmpty(request.requestId) || request.loanAmount == 0 || string.IsNullOrEmpty(request.zipCode))
                    return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Request missing required fields.", apiEndPoint);

                if (authToken != legacyAuthToken)
                {
                    AuthToken authTokenObject = (!string.IsNullOrEmpty(authToken)) ? new AuthToken(authToken) : null;
                    HttpResponseMessage errorResponse = this.Authorize(apiEndPoint, authTokenObject, Partner?.ApiPartnerId ?? 0);
                    if (errorResponse != null)
                        return errorResponse;
                }

                this.CommonProcesses(Request);

                Service service = new Service();
                service.StartTime = startTime;
                service.UserId = CommonParams.UseOnlyThisUserId;
                service.ClientId = Master.Lists.Users.GetUserById(service.UserId)?.ClientId ?? 0;
                service.ApiWebServiceId = ApiWebService.Id;
                service.Endpoint = apiEndPoint;
                service.Route = this.Request?.RequestUri?.AbsolutePath;
                service.ServiceName = ApiWebService.WebServiceName;
                service.CallingAppType = Types.Api.AppType.Api_Partner.ToString();
                service.HttpStatusCode = HttpStatusCode.Accepted.ToString();
                service.CallingIpAddress = ClientInfo.GetIPAddress(this.Request);

                CancellationTokenSource cancelToken = this.StartTimeoutTimer(this.CommonParams.TimeoutInMill);

                //Don't wait for this to process, it uses a callback to send response(s)
                Task.Run(() =>
                {
                    new Quoting.Zillow.V2.ProcessJsonRequest(service, request, cancelToken, CommonParams.UseOnlyThisUserId, CommonParams.DebugModeType);

                    //this is here to help ensure the PreQualUsers DataUpdate is not dead...
                    if (!PreQualUsers.Instance.IsDataUpdateRunning && (DateTime.Now - preQualLastUpdate).TotalMinutes > 10)
                    {
                        preQualLastUpdate = DateTime.Now;
                        PreQualUsers.Instance.StartDataUpdate(PreQualUsers.Config.UpdateDataCheckInterval);
                    }
                }).Wait(cancelToken.Token);

                FullMortgageResponse response = new FullMortgageResponse();
                response.ClientDefinedIdentifier = request.requestId;
                response.ApiEndPoint = apiEndPoint;
                response.ExecutionTimeInMillisec = (DateTime.Now - startTime).TotalMilliseconds;
                ActionContext.Request.Properties[Common.Global.ARequestPropertyName] = response;

                string msg = "OK";
                if (CommonParams.DebugModeType != Processing.DebugModeType.None)
                    msg += "|" + CommonParams.DebugModeType.ToString();
                if (CommonParams.UseOnlyThisUserId > 0)
                    msg += "|" + CommonParams.UseOnlyThisUserId;
                return Request.CreateResponse(HttpStatusCode.OK, msg);
            }
            catch (Exception ex)
            {
                Common.Global.OutPrint(ex.Message, new SimpleLogger.LocationObject(this, "MortgageRequest/POST"), SimpleLogger.LogLevelType.CRITICAL);
                return this.CreateErrorResponse(HttpStatusCode.InternalServerError, "Exeception:"+ ex.Message, apiEndPoint);
            }

            
        }

        /*
        /// <summary>
        /// This Controller is designed to 'Load' or 'Stress' test the Zillow controller. It is set to provide random input for mass requests 
        /// </summary>
        [ApiExplorerSettings(IgnoreApi = true)]
        [EnableThrottling(PerSecond = ratePerSec, PerMinute = ratePerMin, PerHour = 10000)] //limit the load test
        [Route("MortgageRequestLoadTest")]
        [HttpPost]
        public HttpResponseMessage MortgageRequestLoadTest(string authToken, zillowLoanRequestNotification request)
        {
            Random r = new Random();
            request.requestId = "ZR-TEST"+ DateTime.Now.DayOfYear + DateTime.Now.ToString("HHmmssff");

            var r1 = r.Next(0, 20);
            switch (r1)
            {
                case 2: request.zipCode = "66101"; request.stateAbbreviation = "KS"; break; //66101 Kansas City
                case 4: request.zipCode = "04101"; request.stateAbbreviation = "ME"; break; //04101 Portland Maine
                case 6: request.zipCode = "99201"; request.stateAbbreviation = "WA"; break; //99201 Spokane
                case 8: request.zipCode = "99201"; request.stateAbbreviation = "WA"; break; //99201 Spokane
                case 10: request.zipCode = "83642"; request.stateAbbreviation = "ID"; break; //83642 Meridian
                case 12: request.zipCode = "90808"; request.stateAbbreviation = "CA"; break; //90808 LongBeach
                case 14: request.zipCode = "33101"; request.stateAbbreviation = "FL"; break; //33101 Miami
                case 16: request.zipCode = "33101"; request.stateAbbreviation = "FL"; break; //33101 Miami
            }
            if (r1 > 10)
                request.loanToValuePercent = 70;
            if (r1 > 15)
                request.vaEligible = true;
            r1 = (r1 < 10) ? r.Next(10, 30) : r.Next(55, 80);
            request.loanAmount = NullSafe.NullSafeInteger(r1 + "0000");
            request.propertyValue = request.loanAmount + request.downPayment;
            request.loanToValuePercent = request.loanAmount / (decimal)request.propertyValue;
            Debug.WriteLine("MortgageRequestTest: -RequestId:"+ request.requestId +" -State:" + request.stateAbbreviation +" -IsVa:"+ request.vaEligible + " -LoanAmount:"+ request.loanAmount);

            this.CommonParams = new CommonParams(Request) { TimeoutInMill = 60000 };
            return this.MortgageRequest(authToken, request);
        }*/

        [ApiExplorerSettings(IgnoreApi = true)]
        public string CreateDummyRequest(List<KeyValuePair<string,string>> query)
        {
            var isVa = (query.FirstOrDefault(x => x.Key.ToLower() == "isva").Value != null).ToString().ToLower();
            var zipCode = query.FirstOrDefault(x => x.Key.ToLower() == "zipcode").Value ?? "90808";
            var stateAbbv = BusinessObjects.Counties.GetCountyByZipCode(zipCode).FirstOrDefault()?.StateAbbv ?? "CA";
            var loanAmount = NullSafe.NullSafeInteger(query.FirstOrDefault(x => x.Key.ToLower() == "loanamount").Value, 268612);
            var requestId = "ZR-TEST" + DateTime.Now.ToString("MMddHHmmssff");
            var dummyRequest = "{ \"requestId\": \"" + requestId + "\", \"totalAssets\": 0, \"zipCode\": \"" + zipCode + "\", \"stateAbbreviation\": \"" + stateAbbv + "\", \"debtToIncomePercent\": 0, \"annualIncome\": 56550, \"clickPrice\": 0, \"firstTimeBuyer\": false, \"loanAmount\": " + loanAmount + ", \"newConstruction\": false, \"creditScoreRange\": \"R_720_739\", \"loanPurpose\": \"Purchase\", \"vaEligible\": " + isVa + ", \"desiredPrograms\": [\"ARM5\", \"Fixed15Year\", \"Fixed30Year\"], \"propertyUse\": \"Primary\", \"created\": \"2016-02-09T13:15:18.693000-08:00\", \"loanToValuePercent\": 80, \"monthlyDebts\": 0, \"propertyValue\": 282750, \"selfEmployed\": false, \"downPayment\": 14138, \"propertyType\": \"SingleFamilyHome\", \"acceptPrepaymentPenalty\": false, \"hasBankruptcy\": false, \"hasForeclosure\": false}";
            return dummyRequest;
        }

    }
}

