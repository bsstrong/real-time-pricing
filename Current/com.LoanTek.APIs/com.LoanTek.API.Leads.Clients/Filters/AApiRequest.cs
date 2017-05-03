using System;
using com.LoanTek.API.Filters;
using com.LoanTek.API.Requests;

namespace com.LoanTek.API.Leads.Clients.Filters
{
    /// <summary>
    /// Parent <see cref="IApiRequest"/> IApiRequest object holding common param(s).  All other IApiRequest objects extend this. 
    /// </summary>
    public class AApiRequest : IApiRequest
    {
        public AApiRequest()
        {
            this.StartTime = DateTime.Now;
        }

        public long Id { get; set; }
        public DateTime StartTime { get; set; }
        public string ClientDefinedIdentifier { get; set; }
        public string RemoteIP { get; set; }
        public string LocalServerName { get; set; }
        public string ApiName { get; set; }
        public string ApiEndPoint { get; set; }
        public string AuthToken { get; set; }
        public string Url { get; set; }
        public int ClientId { get; set; }
        public string RawRequest { get; set; }

        public bool Save()
        {
            bool update = true;

            //make sure values fit
            if (this.ApiEndPoint?.Length > 999)
                this.ApiEndPoint = this.ApiEndPoint.Substring(0, 999);
            if (this.Url?.Length > 999)
                this.Url = this.Url.Substring(0, 999);

            /*
            using ( dc = new ())
            {
                API_Pricing_Client_Request result = null;
                if(this.Id > 0)
                    result = Queryable.FirstOrDefault<API_Pricing_Client_Request>(dc.API_Pricing_Client_Requests, x => (this.Id > 0 && x.Id == this.Id));
                if (result == null)
                {
                    result = new API_Pricing_Client_Request();
                    dc.API_Pricing_Client_Requests.InsertOnSubmit(result);
                    update = false;
                }
                ClassMappingUtilities.SetPropertiesForTargetWithTypeCheck(this, result, new List<string>(){"Id"});
                if (!string.IsNullOrEmpty(this.RawRequest))
                    result.RawRequest = Compression.GZip(this.RawRequest);

                //check for missing values
                if (result.LocalServerName == null) result.LocalServerName = string.Empty;
                if (result.ApiName == null) result.ApiName = string.Empty;

                dc.SubmitChanges();
                this.Id = result.Id;
            }
            */
            return update;
        }


    }
}