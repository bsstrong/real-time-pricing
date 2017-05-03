using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using com.LoanTek.API.Requests;
using com.LoanTek.Quoting;

namespace com.LoanTek.API.Pricing.Partners.Models.Common
{
    /// <summary>
    /// A Full Mortgage Request contains a <see cref="MortgageLoanRequest"/> MortgageLoanRequest object. 
    /// </summary>
    public class FullMortgageRequest : IPartnerRequest
    {
        #region IClientResponse fields

        /// <summary>
        /// Unique Id provided by the client to track and lookup this request. Commonly referred to as the 'RequestId'.
        /// </summary>
        [Required]
        public string ClientDefinedIdentifier { get; set; }

        /// <summary>
        /// Generic List of objects that the client can pass-through with the request and be returned unaltered in the response.
        /// This can be a single word of information, number(s), a list of strings, or a complex object that the client wants to pass along with the request and be returned in the response. 
        /// </summary>
        public List<object> PassThroughItems { get; set; }

        /// <summary>
        /// The URL to post Quotes to. If null, then Quotes will be returned in the response. 
        /// </summary>
        /// <remarks>
        /// If PostbackInChuncks is set to true but this is null, then PostbackInChuncks is ignored.
        /// </remarks>
        public string PostbackUrl { get; set; }

        /// <summary>
        /// True or False if the postback is to be split up into chunks. Each chunk should contain all the quotes for one Client. 
        /// Each request could have between 0 (no quotes to return) to X (one for each client that has quotes) number of postbacks. 
        /// If this is set to true, then PostbackUrl property MUST contain a valid URL to post the responses to. 
        /// </summary>
        /// <remarks>
        /// If this is set to true but PostbackUrl is null, then this is ignored.
        /// </remarks>
        public bool PostbackInChunks { get; set; }

        #endregion

        /// <summary>
        /// The <see cref="MortgageLoanRequest"/> MortgageLoanRequest contains all the loan request details.
        /// </summary>
        [Required]
        public MortgageLoanRequest LoanRequest { get; set; }

        /// <summary>
        /// A customized Json object representing only the data fields that you want to be returned for each quote in the response. 
        /// The base object used in the response is the <see cref="IQuote"/>IQuote object and therefore the field names in the customized Json object must match the field names of an <see cref="IQuote"/>IQuote object.
        /// </summary>
        public string CustomQuoteResponseJson { get; set; }
    }

}