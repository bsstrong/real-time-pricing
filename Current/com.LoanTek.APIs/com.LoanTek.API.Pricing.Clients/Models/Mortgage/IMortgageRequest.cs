using com.LoanTek.API.Common.Models;

namespace com.LoanTek.API.Pricing.Clients.Models.Common.Mortgage
{
    public interface IMortgageRequest : IClientRequest
    {
        /// <summary>
        /// Contains the members and methods necessary to price a mortgage request with a full set of requirements
        /// </summary>
        MortgageLoanRequest LoanRequest { get; set; }
    }
}