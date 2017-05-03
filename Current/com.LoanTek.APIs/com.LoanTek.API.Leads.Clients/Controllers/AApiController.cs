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
using com.LoanTek.API.Common.Models;
using com.LoanTek.API.Leads.Clients.Models.Common;
using com.LoanTek.Biz.Api.Objects;
using com.LoanTek.CRM.IData;
using com.LoanTek.CRM.Processing;
using com.LoanTek.CRM.Processing.Common;
using com.LoanTek.IData;
using com.LoanTek.Master;
using LoanTek.Utilities;
using Users = com.LoanTek.Master.Lists.Users;

namespace com.LoanTek.API.Leads.Clients.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AApiController : ApiController
    {
        protected readonly Stopwatch Sw = new Stopwatch();
        protected string EndPoint;
        protected AuthToken AuthToken;
        protected CommonParams CommonParams;
        protected string ContentType;

        protected Api TheApiObject;
        public MediaTypeFormatter ResponseFormatter = new JsonMediaTypeFormatter() {SerializerSettings = Global.JsonSettings};

        protected CancellationTokenSource StartTimeoutTimer(int timeoutInMill)
        {
            CancellationTokenSource cancelToken = new CancellationTokenSource();
            Task.Run(async () =>
            {
                await Task.Delay(timeoutInMill);
                cancelToken.Cancel();
            });
            return cancelToken;
        }

        protected HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string message, string apiEndPoint)
        {
            ALeadsResponse response = new ALeadsResponse();
            response.Status = StatusType.Error;
            response.ApiEndPoint = apiEndPoint;
            response.Message = message;
            return Request.CreateResponse(statusCode, response, ResponseFormatter);
        }

        /// <summary>
        /// Internal method that performs checks that should be common to most controller requests
        /// </summary>
        /// <param name="request"></param>
        /// <param name="apiEndPoint"></param>
        /// <returns></returns>
        protected HttpResponseMessage CommonChecks(ALeadsRequest request, string apiEndPoint)
        {
            if (request == null)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Request is invalid.", apiEndPoint);

            if (request.LeadFile == null)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Request missing required Lead File.", apiEndPoint);

            if (request.LeadFile?.ClientId == 0)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Lead File missing required ClientId field", apiEndPoint);

            if (request.LeadFile.Persons?.Count == 0)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Lead File must contain at least one person", apiEndPoint);

            return null;
        }

        /// <summary>
        /// This method can be overriden in a controller class to provide more specific Authorize for certain actions 
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="authTokenObject"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        protected HttpResponseMessage Authorize(string endPoint, AuthToken authTokenObject, int userId)
        {
            if (authTokenObject == null || authTokenObject.ClientId == 0)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid or missing required Auth Token", endPoint);

            if (string.IsNullOrEmpty(authTokenObject.ApiName) || authTokenObject.ClientId < 1)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid or missing required data (AuthToken is invalid)", endPoint);
            if (!endPoint.StartsWith(authTokenObject.ApiName))
                return this.CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid Authentication Token", endPoint);

            AClient client = Master.Clients.GetClientById(authTokenObject.ClientId);
            if (client == null)
                return this.CreateErrorResponse(HttpStatusCode.Forbidden, "Client is invalid", endPoint);
            if (userId > 0 && (client.ClientUsers == null || client.ClientUsers.All(x => x.Id != userId)))
                return this.CreateErrorResponse(HttpStatusCode.Forbidden, "This UserId is not valid for this client", endPoint);

            return null;
        }

        /// <summary>
        /// Internal method that performs processes that should be common to most controller requests
        /// </summary>
        /// <param name="request">HttpRequestMessage</param>
        protected void CommonProcesses(HttpRequestMessage request)
        {
            //can also inherit this class and override default values...
            if (this.CommonParams == null)
                this.CommonParams = new CommonParams(Request);
        }

        protected ALeadsResponse Process(ALeadsRequest request)
        {
            //if the ClientId is not set in the 'File' then use the clientId from the posting URL authToken
            if (request.LeadFile?.ClientId == 0)
                request.LeadFile.ClientId = this.AuthToken.ClientId;

            this.CommonProcesses(Request);

            CancellationTokenSource cancelToken = this.StartTimeoutTimer(CommonParams.TimeoutInMill);

            ProcessFileRequest process = new ProcessFileRequest(request.LeadFile, cancelToken);
            Debug.WriteLine("Done. -Status:" + process.Request.StatusType + " -ActionType:" + process.Request.ActionType + " -In Secs:" + (process.Request.EndTime.GetValueOrDefault() - process.Request.StartTime).TotalSeconds);

            SalesLeadResponse response = new SalesLeadResponse();
            response.ExecutionTimeInMillisec = Sw.ElapsedMilliseconds;
            response.ClientDefinedIdentifier = request.LeadFile?.ClientDefinedIdentifier;
            response.ApiEndPoint = this.EndPoint;
            response.PassThroughItems = request.PassThroughItems;
            if (process.Request == null)
            {
                response.Status = StatusType.Error;
                response.Message = "ProcessFile.Request is missing.";
            }
            else
            {
                response.LoanTekDefinedIdentifier = process.Request?.RequestId;
                response.Message = process.Request.ActionType + "|" + process.Request.Misc?.Replace("||", "|");
                response.Status = process.Request.StatusType;
            }
            return response;
        }

        private static readonly Services services = new Services(DataContextType.Database, DataConnections.DataContextLeadsRead, DataConnections.DataContextLeadsWrite);

        [ApiExplorerSettings(IgnoreApi = true)]
        protected Service CreateAndSaveService(ALeadsRequest request, ALeadsResponse response, string message, Types.Api.AppType appType, ApiWebService webService)
        {
            Service obj = new Service();
            obj.RequestId = response.LoanTekDefinedIdentifier;
            obj.StartTime = DateTime.Now.AddMilliseconds(-this.Sw.ElapsedMilliseconds);
            obj.EndTime = DateTime.Now;
            obj.ClientId = this.AuthToken?.ClientId > 0 ? this.AuthToken.ClientId : Users.GetUserById(request.LeadFile.UserId)?.ClientId ?? 0;
            obj.UserId = request.LeadFile.UserId;
            obj.ApiWebServiceId = webService.Id;
            obj.ServiceName = webService.WebServiceName;
            obj.Endpoint = this.EndPoint;
            obj.Route = this.Request?.RequestUri?.PathAndQuery;
            obj.HttpStatusCodeType = HttpStatusCode.OK;
            obj.Message = message;
            obj.CallingIpAddress = ClientInfo.GetIPAddress(this.Request);
            obj.CallingAppType = appType;
            services.Put(obj);
            return obj;
        }

        /// <summary>
        /// Retrieve data about this Web Service
        /// </summary>
        /// <returns cref="Api">Api object</returns>
        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("Api")]
        [HttpGet]
        public Api GetApi()
        {
            if (TheApiObject.Servers == null)
            {
                var requestUri = this.Request.RequestUri;
                TheApiObject.Servers = new List<Server>()
                {
                    new Server()
                    {
                        ServerName = Global.LocalServerName,
                        Domain = requestUri.Scheme + Uri.SchemeDelimiter + requestUri.Host + (requestUri.IsDefaultPort ? "" : ":" + requestUri.Port),
                        ServerStatus = Global.ServerStatusType
                    }
                };
            }
            return TheApiObject;
        }
    }
}