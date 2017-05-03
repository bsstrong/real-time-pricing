using System.Net.Http;
using com.LoanTek.Biz.Api.Objects;
using com.LoanTek.Biz.Pricing.Objects;
using com.LoanTek.Forms.Mortgage;
using com.LoanTek.Quoting.Mortgage;
using Newtonsoft.Json;

namespace com.LoanTek.API.Pricing.Clients.Models
{
    interface IApiController
    {
        long RatePerSec { get; }
        long RatePerMin { get; }   
        Partner Partner { get; set; }
        ApiWebService ApiWebService { get; set; }
        JsonSerializerSettings JsonSerializerSettings { get; set; }

        HttpResponseMessage GetPreQualList(string authToken, IMortgageRequest<IMortgageForm> request);

        HttpResponseMessage GetCache(bool clear = false, int region = 0);

        HttpResponseMessage Info(string authToken);
    }   
}
