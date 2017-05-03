using System;
using com.LoanTek.API.Instances;
using com.LoanTek.Biz.Api.Objects;

namespace com.LoanTek.API
{
    public class WebService : IInstance
    {
        public WebService() { }

        public WebService(string uri, ApiWebService apiWebService) : base(uri)
        {
            this.ApiWebService = apiWebService;
        }

        public ApiWebService ApiWebService { get; set; }
        public ApiWebServiceVersion ApiWebServiceVersion { get; set; }
    }
}   
