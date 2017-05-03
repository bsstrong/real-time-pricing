using System;
using System.Data.SqlTypes;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using com.LoanTek.API.Requests;
using com.LoanTek.Master.Data.LinqDataContexts;
using LoanTek.Utilities;

namespace com.LoanTek.API.Pricing.Clients.Models
{
    public class AApiResponse : IApiResponse
    {
        public long Id { get; set; }
        public string ClientDefinedIdentifier { get; set; }
        public DateTime EndTime { get; set; }
        public double ExecutionTimeInMillisec { get; set; }
        public string Message { get; set; }
        public HttpStatusCode HttpStatusCode { get; set; }
        public string ApiEndPoint { get; set; }

        public static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, IResponse response)
        {
            return CreateResponse(statusCode, response, new JsonMediaTypeFormatter() {SerializerSettings = Global.JsonSettings});
        }
        public static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, IResponse response, JsonMediaTypeFormatter formatter)
        {
            return new HttpResponseMessage(statusCode) { Content = new ObjectContent(response.GetType(), response, formatter) };
        }

        public bool Save()
        {
            bool update = true;
            using (WebAPIsDataContext dc = new WebAPIsDataContext())
            {
                API_Pricing_Client_Request result = null;
                if (this.Id > 0)
                    result = dc.API_Pricing_Client_Requests.FirstOrDefault(x => (this.Id > 0 && x.Id == this.Id));
                if (result == null)
                {
                    result = new API_Pricing_Client_Request();
                    result.StartTime = SqlDateTime.MinValue.Value;
                    result.RawRequest = Compression.GZip("ERROR: No ARequest created by API.");
                    result.LocalServerName = Global.LocalServerName;
                    result.ApiName = Global.ApiObject.ApiName;
                    dc.API_Pricing_Client_Requests.InsertOnSubmit(result);
                    update = false;
                }
                if (string.IsNullOrEmpty(result.ClientDefinedIdentifier)) //if the original request doesn't have a value for ClientDefinedIdentifier, use the response value
                    result.ClientDefinedIdentifier = this.ClientDefinedIdentifier;
                if (!string.IsNullOrEmpty(this.ApiEndPoint)) //use the response value for the endpoint if one exists
                    result.ApiEndPoint = this.ApiEndPoint;
                result.ExecutionTimeInMillisec = (result.ExecutionTimeInMillisec == 0) ? -1 : this.ExecutionTimeInMillisec;
                result.ResponseMessage = this.Message;
                result.ResponseStatusCode = (short?)((int)this.HttpStatusCode);

                dc.SubmitChanges();
            }
            return update;
        }

    }


}