using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using com.LoanTek.API.Common.Models;
using com.LoanTek.API.Filters;
using com.LoanTek.API.Leads.Clients.Filters;
using com.LoanTek.API.Leads.Clients.Models.Common;
using com.LoanTek.API.Requests;
using com.LoanTek.Biz.Api.Objects;
using com.LoanTek.CRM.Files;
using com.LoanTek.CRM.Processing;
using com.LoanTek.CRM.Processing.Common;
using LoanTek.LoggingObjects;
using LoanTek.Utilities;
using Newtonsoft.Json;
using WebApiThrottle;

namespace com.LoanTek.API.Leads.Clients.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class NewLeadsController : AApiController
    {
        public static Api ApiObject;

        //Static constructor is used to initialize any static data or to perform a particular action that needs to be preformed once only
        static NewLeadsController()
        {
            #region API object

            ApiObject = new Api();
            ClassMappingUtilities.SetPropertiesForTarget(Global.ApiObject, ApiObject);
            ApiObject.WebServiceName = "NewLeads";
            ApiObject.ApiName += ".Leads";
            ApiObject.RoutePrefix = Global.ApiObject.RoutePrefix.Replace("{version}", Global.ApiObject.Versions.LastOrDefault()?.MajorVersionId.ToString() + ".0"); //don't specify the minor verison in routing...
            ApiObject.Route = "{endpoint}/{authToken}";
            ApiObject.RequestObjectType = typeof(ALeadsRequest);
            ApiObject.RequestObjectType = typeof(ALeadsResponse);
            ApiObject.ResponsePostbackObjectType = null;
            ApiObject.RateLimited = true;
            ApiObject.Versions = Global.ApiObject.Versions;

            #endregion
        }

        /// <summary>
        /// Used to query information about this particular web service. 
        /// </summary>
        /// <param name="authToken">Unique secuity token required for accessing these web services. A partner / client can get their token from the developers portal.</param>
        /// <returns cref="Api">Api object containing details about this web service.</returns>
        [Route("SalesLeadRequest/Add/Info")]
        [HttpGet]
        public Api SalesLeadRequestAddInfo(string authToken)
        {
            var apiDesc = Global.ApiExplorer.ApiDescriptions.FirstOrDefault(x => x.RelativePath.Equals(ApiObject.Route + "/" + this.ActionContext.ActionDescriptor.ActionName.Replace("Info", "")));
            if (apiDesc == null)
                throw new Exception("Failed to find ApiDescription for " + this.ActionContext.ActionDescriptor.ActionName);
            var endPoint = new Api(apiDesc);
            ClassMappingUtilities.SetNullOrEmptyOnlyPropertiesForTarget(ApiObject, endPoint);
            endPoint.Servers = ApiObject.Servers ?? ApiObject.SetServer(this.Request.RequestUri, Common.Global.LocalServerName, Common.Global.ServerStatusType);
            endPoint.ResponseQuoteObjectType = null;
            return endPoint;
        }

        /// <summary>
        /// Used for testing the NewLeadRequest Web Service.
        /// </summary>
        /// <remarks>
        /// Generates a dummy FullLeadRequest object to process and responds with a FullLeadResponse object.
        ///</remarks>
        /// <param name="authToken">Url Param {authToken} is unique to each user and web service and can be found on each individual web service page in the developers website after logging in.</param>
        /// <returns></returns>
        [EnableThrottling(PerMinute = 10, PerHour = 50)]//limits for testing
        //[Filters.PreProcessFilter] //does general PRE-processing for a web service call
        //[Filters.PostProcessFilter] //does general POST-processing for a web service call[Route("FullLeadRequestTest")]
        [Route("SalesLeadRequestTest/Add/{authToken}")]
        [HttpGet] //used for testing
        [ResponseType(typeof(SalesLeadResponse))]
        public HttpResponseMessage SalesLeadRequestAddTest(string authToken)
        {
            string apiEndPoint = ApiObject.ApiName + ".SalesLeadRequestTest/GET";
            this.CommonParams = new CommonParams(Request) { TimeoutInMill = 60000 };
            //SalesLeadRequest request = new SalesLeadRequest();
            //SalesLeadRequest request = JsonConvert.DeserializeObject<SalesLeadRequest>("{\"LeadFile\":{\"ClientId\":399,\"FileType\":\"SalesLead\",\"Reason\":\"\",\"Source\":{\"Active\":true,\"Alias\":\"LoanTek.com\",\"Id\":44,\"SourceType\":\"LeadSource\",\"Name\":\"LoanTek.com\",\"SubName\":\"Contact - Us - Form\"},\"NotifyUserOfNewLead\":true,\"SendNewLeadInitialWelcomeMessage\":true,\"ClientDefinedIdentifier\":\"LTWS1462220067420\",\"Persons\":[{\"PersonCategoryType\":\"Person\",\"PersonType\":\"Primary\",\"FirstName\":\"Eric\",\"LastName\":\"Tjemsland\"}],\"MiscData\":[{\"Name\":\"AdditionalInformation\",\"Value\":\"\"},{\"Name\":\"PromoCode\",\"Value\":\"LPP01\"}]}}");
            var json = File.ReadAllText(@"D:\LoanTek\Current\com.LoanTek.APIs\com.LoanTek.API.Leads.Clients\_TestData\SalesLeadRequest.json");
            SalesLeadRequest request = JsonConvert.DeserializeObject<SalesLeadRequest>(json, Global.JsonSettings);
            return this.SalesLeadRequestAdd(authToken, request);
        }

        [Route("SalesLeadRequest/Add/{authToken}")]
        [HttpPost] //used for testing
        [ResponseType(typeof(SalesLeadResponse))]
        public HttpResponseMessage SalesLeadRequestAdd(string authToken, SalesLeadRequest request)
        {
            string apiEndPoint = ApiObject.ApiName + ".SalesLeadRequest/POST";
            this.EndPoint = apiEndPoint;
            try
            {
                var startTime = DateTime.Now;

                this.ContentType = this.Request.Content?.Headers?.ContentType?.MediaType;

                HttpResponseMessage errorResponse = this.CommonChecks(request, apiEndPoint);
                if (errorResponse != null)
                    return errorResponse;

                AuthToken authTokenObject = (!string.IsNullOrEmpty(authToken)) ? new AuthToken(authToken) : null;
                errorResponse = this.Authorize(apiEndPoint, authTokenObject, request.LeadFile?.UserId ?? 0);
                if (errorResponse != null)
                    return errorResponse;

                //if the ClientId is not set in the 'File' then use the clientId from the posting URL authToken
                if(request.LeadFile.ClientId == 0)
                    request.LeadFile.ClientId = authTokenObject.ClientId;
                
                //TODO - check client for license...
                //if(!License.Has(LoanPricerAPILicenseType, authTokenObject.ClientId))
                //    return this.CreateErrorResponse(HttpStatusCode.PaymentRequired, "Missing valid license:"+ LoanPricerAPILicenseType.ToString(), apiEndPoint);

                this.CommonProcesses(Request);

                CancellationTokenSource cancelToken = this.StartTimeoutTimer(CommonParams.TimeoutInMill);

                ProcessFileRequest process = new ProcessFileRequest(request.LeadFile, cancelToken);
                Debug.WriteLine("Done. -Status:" + process.Request.StatusType + " -ActionType:" + process.Request.ActionType + " -In Secs:" + (process.Request.EndTime.GetValueOrDefault() - process.Request.StartTime).TotalSeconds);
            
                SalesLeadResponse response = new SalesLeadResponse();
                response.ExecutionTimeInMillisec = (DateTime.Now  - startTime).TotalMilliseconds;
                response.LoanTekDefinedIdentifier = StringUtilities.UniqueId();
                response.ApiEndPoint = apiEndPoint;
                //response.CachedId = processor.Request.CachedId;
                response.ClientDefinedIdentifier = request.LeadFile.ClientDefinedIdentifier;
                response.PassThroughItems = request.PassThroughItems;
                if (process.Request == null)
                {
                    response.Status = StatusType.Error;
                    response.Message = "ProcessFile.Request is missing.";
                }
                else
                {
                    switch (process.Request.StatusType)
                    {
                        case StatusType.Complete:response.Status = StatusType.Complete; break;
                        case StatusType.Cancelled:response.Status = StatusType.Cancelled; break;
                        default:  response.Status = StatusType.Error; break;
                    }
                    response.Message = process.Request.ActionType + "|" + process.Request.Misc.Replace("||", "|");
                }

                Types.Api.AppType appType = (request.LeadFile?.Source?.Name?.ToLower().Contains("widget") ?? false) || (request.LeadFile?.Source?.SubName?.ToLower().Contains("widget") ?? false)
                    ? Types.Api.AppType.Widget
                    : Types.Api.AppType.Api_Clients;
                Task.Run(() => this.CreateAndSaveService(request, response, response.Message, appType, new ApiWebService { WebServiceName = "SalesLeadRequest", EndPoint = this.EndPoint })).ConfigureAwait(false);

                return AApiResponse.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception ex)
            {
                Global.OutPrint(ex.Message, new SimpleLogger.LocationObject(this, apiEndPoint), SimpleLogger.LogLevelType.CRITICAL);
                return this.CreateErrorResponse(HttpStatusCode.InternalServerError, "Exception:" + ex.Message, apiEndPoint);
            }
        }

        /* EXAMPLES
        // GET: api/Leads
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Leads/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Leads
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/Leads/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Leads/5
        public void Delete(int id)
        {
        }
        */
    }
}
