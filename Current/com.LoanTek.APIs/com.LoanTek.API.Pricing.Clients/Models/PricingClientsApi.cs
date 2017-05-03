using System;
using System.Collections.Generic;

namespace com.LoanTek.API.Pricing.Clients.Models
{
    public class PricingClientsApi : Api
    {
        public PricingClientsApi()
        {
            this.WebServiceName = null;
            this.ApiName = "com.LoanTek.API.Pricing.Clients";
            this.RoutePrefix = "Pricing/Client/{versionId}/";
            this.Route = null;
            this.Formats = new List<FormatType>() { FormatType.JSON, FormatType.XML };
            this.RequestObjectType = typeof (IRequest);
            this.ResponseObjectType = typeof(IResponse);
            this.Protocal = "REST/Http";
            this.AuthRequired = true;
            this.RateLimited = true;
            this.RateLimitPerMinute = 60;
            this.Versions = new List<Version>()
            {
                new Version()
                {
                    MajorVersionId = 1, MinorVersionId = 0, VersionStatus = Types.Api.VersionStatusType.Beta, Created = DateTime.Now.AddDays(-10), LastUpdated = DateTime.Now.AddDays(-1)
                }
                
            };
        }
    }

}
