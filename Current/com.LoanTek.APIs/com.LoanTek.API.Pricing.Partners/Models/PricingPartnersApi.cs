using System;
using System.Collections.Generic;
using com.LoanTek.API.Pricing.Partners.Filters;

namespace com.LoanTek.API.Pricing.Partners.Models
{
    public class PricingPartnersApi : Api
    {
        public PricingPartnersApi()
        {
            this.ApiName = "com.LoanTek.API.Pricing.Partners";
            this.RoutePrefix = "{versionId}";
            this.Route = "/Common/{authToken}/{partnerId}";
            this.Formats = new List<FormatType>() {FormatType.JSON, FormatType.XML};
            this.RequestObjectType = new AApiRequest().GetType();
            this.ResponseObjectType = new ApiResponse().GetType();
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
