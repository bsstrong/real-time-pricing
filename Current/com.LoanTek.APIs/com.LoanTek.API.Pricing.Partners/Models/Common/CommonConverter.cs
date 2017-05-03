using System.Collections.Generic;
using com.LoanTek.API.Requests;
using com.LoanTek.Quoting;
using com.LoanTek.Quoting.Common;

namespace com.LoanTek.API.Pricing.Partners.Models.Common
{
    public class CommonConverter : Converter
    {
        public new class LoanQuoteSubmission : IQuoteSubmission<MortgageLoanQuote>
        {
            public string RequestId { get; set; }
            public QuotingUser QuotingUser { get; set; }
            public List<MortgageLoanQuote> Quotes { get; set; }
        }

        public override string GenerateQuoteId(string s)
        {
            return "LTPQ" + s;
        }

        
    }
}