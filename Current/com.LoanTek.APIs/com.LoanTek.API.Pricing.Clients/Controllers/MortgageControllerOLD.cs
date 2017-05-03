using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Description;
using com.LoanTek.API.Common.Models;
using com.LoanTek.API.Pricing.Clients.Models.Common.Mortgage;
using com.LoanTek.API.Pricing.Partners.Models;
using com.LoanTek.API.Requests;
using com.LoanTek.Quoting.Common;
using LoanTek.LoggingObjects;
using LoanTek.Utilities;
using Newtonsoft.Json;
using WebApiThrottle;
using AApiController = com.LoanTek.API.Pricing.Clients.Models.AApiController;
using FullMortgageRequest = com.LoanTek.API.Pricing.Partners.Models.Common.FullMortgageRequest;
using FullMortgageResponse = com.LoanTek.API.Pricing.Partners.Models.Common.FullMortgageResponse;

namespace com.LoanTek.API.Pricing.Clients.Controllers
{
    /// <summary>
    /// Web Service(s) for Quoting a Mortgage Loan Request
    /// </summary>
    //[RoutePrefix("Pricing/Client/{versionId}/Mortgage")] 'Pricing/Client/{versionId}' is defined as a path on the server, so it is not needed here
    [RoutePrefix("Mortgage")]
    public class MortgageControllerOLD : AApiController
    {
        public static Api ApiObject;

        //Static constructor is used to initialize any static data or to perform a particular action that needs to be preformed once only
        static MortgageControllerOLD()
        {
            ApiObject = new Api();
            ClassMappingUtilities.SetPropertiesForTarget(Global.ApiObject, ApiObject);
            ApiObject.WebServiceName = "FIX";
            ApiObject.ApiName += ".Mortgage"; 
            ApiObject.Route = "Mortgage/{controllerName}/{authToken}/{userId}/";
            //ApiObject.Formats = new List<FormatType>() { FormatType.JSON, FormatType.XML };
            ApiObject.RequestObjectType = typeof(FullMortgageRequest);
            ApiObject.ResponseObjectType = typeof(FullMortgageResponse);
            ApiObject.Versions = new List<Version>()
            {
                new Version()
                    {
                        MajorVersionId = 1,
                        MinorVersionId = 0,
                        VersionStatus = Version.VersionStatusType.Beta,
                        Created = Convert.ToDateTime("03/18/2016"),
                        LastUpdated = Convert.ToDateTime("03/18/2016")
                    
                }
            };
        }

        /// <summary>
        /// Retrieve data about this Web Service
        /// </summary>
        /// <returns cref="Api">Api object</returns>
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[Route("Api")]
        //[HttpGet]    
        //public Api GetApi()
        //{
        //    if (ApiObject.Servers == null)
        //    {
        //        var requestUri = this.Request.RequestUri;
        //        ApiObject.Servers = new List<Server>()
        //        {
        //            new Server()
        //            {
        //                ServerName = Global.LocalServerName,
        //                Domain = requestUri.Scheme + Uri.SchemeDelimiter + requestUri.Host + (requestUri.IsDefaultPort ? "" : ":" + requestUri.Port),
        //                ServerStatus = Global.ServerStatusType
        //            }
        //        };
        //    }
        //    return ApiObject;
        //}


        #region FullMortgageRequest

        /// <summary>
        /// Used for testing the FullMortgageRequest Web Service.
        /// </summary>
        /// <remarks>
        /// Generates a dummy MortgageLoanRequest object to process and responds with a FullMortgageResponse object.
        ///</remarks>
        /// <param name="authToken">Url Param {authToken} is unique to each user and web service and can be found on each individual web service page in the developers website after logging in.</param>
        /// <param name="userId">Url Param {userId} is the unique LoanTek UserId used for pricing.</param>
        /// <returns></returns>
        [EnableThrottling(PerMinute = 10, PerHour = 50)]//limits for testing
        //[Filters.PreProcessFilter] //does general PRE-processing for a web service call
        //[Filters.PostProcessFilter] //does general POST-processing for a web service call[Route("FullMortgageRequestTest")]
        [Route("FullMortgageRequestTest/{authToken}/{userId}")]     
        [HttpGet] //used for testing
        [ResponseType(typeof(FullMortgageResponse))]
        public HttpResponseMessage FullMortgageRequestTest(string authToken, int userId)
        {
            FullMortgageRequest dummyRequest = this.createDummyRequest();
            this.CommonParams = new CommonParams(Request) { TimeoutInMill = 60000 };
            return this.FullMortgageRequest(authToken, userId, dummyRequest);
            //return new HttpResponseMessage(HttpStatusCode.OK) { ReasonPhrase = "FullMortgageRequestTest hit" };
        }

        /// <summary>
        /// Web Service for Quoting a Full Mortgage Loan Request. 
        /// </summary>
        /// <remarks>
        /// Processes a FullMortgageRequest object containing a MortgageLoanRequest object and responds with a FullMortgageResponse object.
        ///</remarks>
        /// <example>{host}/Pricing/Client/{versionId}/Mortgage/{authToken}/{userId}/FullMortgageRequest</example>
        /// <param name="authToken">Url Param {authToken} is unique to each user and web service and can be found on each individual web service page in the developers website after logging in.</param>
        /// <param name="userId">Url Param {userId} is the unique LoanTek UserId used for pricing.</param>
        /// <param name="request">A FullMortgageRequest object.</param>
        /// <returns cref="FullMortgageResponse">On success a FullMortgageResponse object with a list of Quotes. Else, a string error message.</returns>
        /// <response code="200">FullMortgageResponse object</response>
        /// <response code="400">Missing required fields or data</response>
        /// <response code="401">Invalid Authentication Token</response>
        /// <response code="403">Invalid Authentication Token (Client Id)</response>
        [EnableThrottling(PerSecond = 5, PerMinute = 60)] //enables Rate Limits
        [Filters.PreProcessFilter] //does general PRE-processing for a web service call
        [Filters.PostProcessFilter] //does general POST-processing for a web service call 
        [Route("FullMortgageRequest/{authToken}/{userId}")]
        [HttpPost]
        [ResponseType(typeof(FullMortgageResponse))]
        public HttpResponseMessage FullMortgageRequest(string authToken, int userId, FullMortgageRequest request)
        {
            string apiEndPoint = ApiObject.ApiName + ".FullMortgageRequest/POST";
            try
            {
                var startTime = DateTime.Now;

                this.ContentType = this.Request.Content?.Headers?.ContentType?.MediaType;
                
                HttpResponseMessage errorResponse = this.CommonChecks(request, apiEndPoint);
                if (errorResponse != null)
                    return errorResponse;

                AuthToken authTokenObject = (!string.IsNullOrEmpty(authToken)) ? new AuthToken(authToken) : null;
                errorResponse = this.Authorize(apiEndPoint, authTokenObject, userId);
                if (errorResponse != null)
                    return errorResponse;

                //TODO - check client for license...
                //if(!License.Has(LoanPricerAPILicenseType, authTokenObject.ClientId))
                //    return this.CreateErrorResponse(HttpStatusCode.PaymentRequired, "Missing valid license:"+ LoanPricerAPILicenseType.ToString(), apiEndPoint);

                this.CommonProcesses(Request);

                CancellationTokenSource cancelToken = this.StartTimeoutTimer(CommonParams.TimeoutInMill);
  
                ProcessRequest processor = new ProcessRequest(new CommonConverter(), PreQualUsers.Instance, Cache.Instance);
                processor.Process(request.LoanRequest, JsonConvert.SerializeObject(request), cancelToken, CommonParams.UseOnlyThisUserId, CommonParams.DebugModeType);

                FullMortgageResponse response = new FullMortgageResponse();
                response.Status = processor.Request.StatusType;
                response.LoanTekDefinedIdentifier = processor.Request.Id;
                response.CachedId = processor.Request.CachedId;
                response.Submissions = processor.Submissions.Select(x => x.LoanQuoteSubmission).ToList();
                response.Message = processor.Request.Misc;             
                response.ApiEndPoint = apiEndPoint;
                response.ClientDefinedIdentifier = request.ClientDefinedIdentifier;
                response.PassThroughItems = request.PassThroughItems;
                response.ExecutionTimeInMillisec = Math.Round((DateTime.Now - startTime).TotalMilliseconds, 4);

                //ActionContext.Request.Properties[Global.ARequestPropertyName] = response; //only needed if going to use a filter to save or log the response
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception ex)
            {
                Global.OutPrint(ex.Message, new SimpleLogger.LocationObject(this, apiEndPoint), SimpleLogger.LogLevelType.CRITICAL);
                return this.CreateErrorResponse(HttpStatusCode.InternalServerError, "Exception:" + ex.Message, apiEndPoint);
            }
        }

        #endregion


        #region private

        private FullMortgageRequest createDummyRequest()
        {
            FullMortgageRequest dummyRequest = new FullMortgageRequest();
            dummyRequest.PostbackInChunks = false;
            dummyRequest.PassThroughItems = new List<object>() { new { Item = "A Pass Through Item" } };
            dummyRequest.LoanRequest = new MortgageLoanRequest();
            ClassMappingUtilities.SetPropertiesForTarget(DummyData.GetRequest(), dummyRequest.LoanRequest);
            dummyRequest.ClientDefinedIdentifier = "LTP" + StringUtilities.UniqueId();
            return dummyRequest;
        }

        #endregion

    }
}
