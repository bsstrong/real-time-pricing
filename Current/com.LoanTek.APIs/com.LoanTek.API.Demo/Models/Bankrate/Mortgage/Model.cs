using System.Collections.Generic;
using System.Net;
using com.LoanTek.API.Demo.Models.Sdk;
using com.LoanTek.Quoting.Mortgage.Bankrate;
using LoanTek.Pricing.BusinessObjects;
using LoanTek.Utilities;

namespace com.LoanTek.API.Demo.Models.Bankrate.Mortgage
{
    public class Model
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        public bool IsSuccessStatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public List<MortgageQuote> Content { get; set; }
        public double TimeInMillisecondsFromSubmit { get; set; }
        public double TimeInMillisecondsToProcess { get; set; }
        public double TimeInMillisecondsToProcessServer { get; set; }
        public int UserId { get; set; } 
        public bool IsCached { get; set; }
        public PricingClientVersionType PricingEngineVersionType { get; set; }
        public List<DateAndTime.TimeObject2> RequestTimeObjects { get; set; }
        public List<DateAndTime.TimeObject2> SubmissionTimeObjects { get; set; }
    }
}