using System.Collections.Generic;
using com.LoanTek.Quoting.Mortgage.Common;

namespace com.LoanTek.API.Pricing.Partners.Models.Mortgage
{
    /// <summary>
    /// A Full Mortgage Request inherits and wraps a <see cref="MortgageRequest"/> MortgageRequest object. 
    /// </summary>
    public class FullMortgageRequest : MortgageRequest
    {
        /// <summary>
        /// Generic List of objects that the client can pass-through with the request and be returned unaltered in the response.
        /// This can be a single word of information, number(s), a list of strings, or a complex object that the client wants to pass along with the request and be returned in the response. 
        /// </summary>
        public List<object> PassThroughItems { get; set; }

        /// <summary>
        /// A customized Json object representing only the data fields that you want to be returned for each quote in the response. 
        /// The base object used in the response is the <see cref="MortgageQuote"/>MortgageQuote object and therefore the field names in the customized Json object must match the field names of an <see cref="MortgageQuote"/>MortgageQuote object.
        /// </summary>
        public string CustomQuoteResponseJson { get; set; }
    }

}