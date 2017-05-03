using System;
using System.Collections.Generic;
using com.LoanTek.API.Leads.Clients.Models.Common;
using com.LoanTek.API.Requests;

namespace com.LoanTek.API.Leads.Clients.Models
{
    public class LeadsClientsApi : Api
    {
        public LeadsClientsApi()
        {
            this.ApiName = "com.LoanTek.API.Leads.Clients";
            this.RoutePrefix = "Leads.Clients/{versionId}/";
            this.Route = null;
            this.Formats = new List<FormatType>() { FormatType.JSON, FormatType.XML };
            this.RequestObjectType = typeof(ALeadsRequest);
            this.ResponseObjectType = typeof(ALeadsResponse);
            this.Protocal = "REST/Http";
            this.AuthRequired = true;
            this.RateLimited = true;
        }
    }

}
