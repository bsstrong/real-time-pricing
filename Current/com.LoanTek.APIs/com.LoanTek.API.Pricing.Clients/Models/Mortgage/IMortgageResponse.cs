using System.Collections.Generic;
using com.LoanTek.API.Common.Models;
using com.LoanTek.Quoting.Common;
using LoanTek.Pricing.BusinessObjects;

namespace com.LoanTek.API.Pricing.Clients.Models.Common.Mortgage
{
    public interface IMortgageResponse : IClientResponse
    {
        /// <summary>
        /// Detailed Information about the county. 
        /// </summary>
        /// <remarks>
        /// The county corresponds to the ZipCode (Or StateAbbreviation and CountyName) passed in by the loan request. 
        /// </remarks>
        ACounty CountyDetails { get; set; }

        List<MortgageLoanQuote> Quotes { get; set; }
    }
}