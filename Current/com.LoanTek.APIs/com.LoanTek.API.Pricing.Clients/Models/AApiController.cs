using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using com.LoanTek.API.Pricing.Clients.Models.Deposit;
using com.LoanTek.API.Pricing.Clients.Models.Loan;
using com.LoanTek.API.Pricing.Partners.Models.Mortgage;
using com.LoanTek.Biz.Api.Objects;
using com.LoanTek.Forms.Loan;
using com.LoanTek.Master;
using com.LoanTek.Quoting.Loans;
using com.LoanTek.Quoting.Mortgage.IData;
using LoanTek.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#pragma warning disable 1591

namespace com.LoanTek.API.Pricing.Clients.Models
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class AApiController : Partners.Models.AApiController
    {
        protected string ContentType;
        protected new ApiWebServiceEndPoint EndPoint;
        protected Types.Api.AppType AppType;
        protected AuthToken AuthToken;
        protected AUser AUser;
        protected readonly Stopwatch Stopwatch = new Stopwatch();
            
        protected HttpResponseMessage CommonChecks(DepositApiRequest request, string apiEndPoint)
        {
            if (request == null)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Request missing required Api Request.", apiEndPoint);

            if (string.IsNullOrEmpty(request.ClientDefinedIdentifier))
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Request missing required ClientDefinedIdentifier.", apiEndPoint);

            if (request.ClientDefinedIdentifier.Length < 8)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Required ClientDefinedIdentifier must be a unique number between 8 and 40 characters and/or digits.", apiEndPoint);

            if (request.DepositRequest?.Form == null)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Request missing required Deposit Request.", apiEndPoint);

            var errors = request.DepositRequest.HasErrors();
            if (errors?.Count > 0)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Deposit Request has missing or invalid data:" + string.Join(";", errors), apiEndPoint + ".");

            return null;
        }

        protected HttpResponseMessage CommonChecks<T,TT>(LoanApiRequest<T, TT> request, string apiEndPoint) where T : ILoanRequest<TT> where TT : ILoanForm
        {
            if (request == null)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Request missing required Api Request.", apiEndPoint);

            if (string.IsNullOrEmpty(request.ClientDefinedIdentifier))
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Request missing required ClientDefinedIdentifier.", apiEndPoint);

            if (request.ClientDefinedIdentifier.Length < 8)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Required ClientDefinedIdentifier must be a unique number between 8 and 40 characters and/or digits.", apiEndPoint);

            if (request.LoanRequest == null || request.LoanRequest.Form == null)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Request missing required Loan Request.", apiEndPoint);

            var errors = request.LoanRequest.HasErrors();
            if (errors?.Count > 0)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Loan Request has missing or invalid data:" + string.Join(";", errors), apiEndPoint + ".");

            return null;
        }

        protected HttpResponseMessage SetApiAndEndPoint(ApiWebService apiWebService, string endPoint)
        {   
            this.EndPoint = apiWebService?.GetEndPoints().FirstOrDefault(x => x.EndPoint == (endPoint?.IndexOf("/") == -1 ? endPoint : endPoint?.Substring(0, endPoint.IndexOf("/"))));
            if (this.EndPoint == null || apiWebService == null)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing Web Service / Endpoint", endPoint);
            if (this.EndPoint.VersionStatusType == Types.Api.VersionStatusType.Removed || apiWebService.ApiStatusType == Types.Api.ApiStatusType.Inactive)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "In-active Web Service / Endpoint", endPoint);
            return null;
        }

        protected HttpResponseMessage Authorize(string endPoint, string authToken, int userId)
        {
            return this.Authorize(endPoint, (!string.IsNullOrEmpty(authToken) ? new AuthToken(authToken) : null), userId);
        }
        protected override HttpResponseMessage Authorize(string endPoint, AuthToken authTokenObject, int userId)
        {
            this.AuthToken = authTokenObject;

            if (authTokenObject == null)    
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid or missing required Auth Token.", endPoint);
            if (userId < 1)
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing UserId in Loan Request", endPoint);
            if (string.IsNullOrEmpty(authTokenObject.ApiName) || authTokenObject.ClientId < 1)
                return this.CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid Authentication Token.", endPoint);
            if (!endPoint.StartsWith(authTokenObject.ApiName))
                return this.CreateErrorResponse(HttpStatusCode.Unauthorized, "Unauthorized Authentication Token.", endPoint);
            this.AUser = (userId == 0) ? Users.GetUsersByClientId(authTokenObject.ClientId)?.FirstOrDefault() : Users.GetUserById(userId);
            if (AUser?.ClientId != authTokenObject.ClientId)
                return this.CreateErrorResponse(HttpStatusCode.Forbidden, "ClientId is invalid for this request.", endPoint);
            return null;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public List<string> GetPropertiesToSerialize<T>(string customQuoteJson)
        {
            if (string.IsNullOrEmpty(customQuoteJson))
                this.PropertiesToSerialize = null;
            return this.PropertiesToSerialize ?? (this.PropertiesToSerialize = JObject.Parse(customQuoteJson ?? JsonConvert.SerializeObject(default(T), new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Include, NullValueHandling = NullValueHandling.Include, ReferenceLoopHandling = ReferenceLoopHandling.Ignore }))?.Children().Select(child => child.Path).ToList());
        }

        public Service CreateService(FullMortgageRequest request, ApiWebService apiWebService, string endPoint)
        {
            Service service = new Service();
            service.StartTime = DateTime.Now.AddMilliseconds(-Stopwatch.ElapsedMilliseconds);
            service.UserId = request.UserId;
            service.ClientId = this.AuthToken?.ClientId ?? -1;
            service.RequestId = request.ClientDefinedId;
            service.ApiWebServiceId = apiWebService.Id;
            service.Endpoint = endPoint ?? this.EndPoint?.EndPoint ?? apiWebService.GetEndPoints().FirstOrDefault()?.EndPoint;  
            service.Route = apiWebService.Domain + "/authkey/" + this.EndPoint?.EndPoint;
            service.ServiceName = apiWebService.WebServiceName;
            service.CallingAppType = this.AppType;
            service.HttpStatusCodeType = HttpStatusCode.Accepted;
            service.CallingIpAddress = ClientInfo.GetIPAddress(this.Request);
            return service;
        }
    }
}