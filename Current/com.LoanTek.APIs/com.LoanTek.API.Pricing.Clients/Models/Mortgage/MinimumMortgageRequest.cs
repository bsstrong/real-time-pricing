using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LoanTek.Pricing.LoanRequests;

namespace com.LoanTek.API.Pricing.Clients.Models.Common.Mortgage
{
    /// <summary>
    /// A Minimum Mortgage Request contains a <see cref="MinimumLoanRequest"/> MinimumLoanRequest object. 
    /// This contains the bare minimum set of data fields to price a loan. 
    /// Useful for getting a quick and more basic list of quotes based on just the most common and primary loan request data fields required.
    /// </summary>
    public class MinimumMortgageRequest : IClientRequest
    {
        #region IClientResponse fields

        /// <summary>
        /// Unique Id provided by the client to track and lookup this request.
        /// </summary>
        [Required]
        public string ClientDefinedIdentifier { get; set; }

        /// <summary>
        /// Generic List of objects that the client can pass-through with the request and be returned unaltered in the response.
        /// This can be a single word of information, number(s), a list of strings, or a complex object that the client wants to pass along with the request and be returned in the response. 
        /// </summary>
        public List<object> PassThroughItems { get; set; }

        #endregion

        /// <summary>
        /// The <see cref="MinimumLoanRequest"/> MinimumLoanRequest contains a bare minimum set of data fields to price a loan.
        /// </summary>
        [Required]
        public MinimumLoanRequest LoanRequest { get; set; }
    }

}