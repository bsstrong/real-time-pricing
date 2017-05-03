using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using com.LoanTek.API.Common.Models;
using com.LoanTek.Quoting;

namespace com.LoanTek.API.Pricing.Clients.Models.Common.Mortgage
{

    /// <summary>
    /// A Custom Mortgage Request contains a <see cref="MortgageLoanRequest"/> MortgageLoanRequest object and a customized Json string representing the data fields you want to be returned in the response.
    /// </summary>
    public class CustomMortgageRequest : IMortgageRequest
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
        /// The <see cref="MortgageLoanRequest"/> MortgageLoanRequest contains all the loan request details.
        /// </summary>
        [Required]
        public MortgageLoanRequest LoanRequest { get; set; }

        /// <summary>
        /// A customized Json object representing only the data fields that you want to be returned in the response. 
        /// The base object used in the response is the <see cref="IQuote"/>IQuote object and therefore the field names in the customized Json object must match the field names of an <see cref="IQuote"/>IQuote object.
        /// </summary>
        public string CustomLoanResponseJson { get; set; }
    }
}