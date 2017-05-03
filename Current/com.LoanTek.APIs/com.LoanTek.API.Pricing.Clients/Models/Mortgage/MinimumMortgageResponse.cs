using System.Collections.Generic;
using com.LoanTek.Quoting;
using LoanTek.Pricing.LoanRequests;

namespace com.LoanTek.API.Pricing.Clients.Models.Common.Mortgage
{
    /// <summary>
    /// Retures a List of <see cref="com.LoanTek.Quoting.IQuote"/>IQuote objects based on the data points of the passed in <see cref="MinimumLoanRequest"/> MinimumLoanRequest object. 
    /// </summary>
    public class MinimumMortgageResponse : IClientResponse
    {
        #region IClientResponse fields

        /// <summary>
        /// Unique Id provided by the client to track and lookup this request. Commonly referred to as the 'RequestId'.
        /// </summary>
        public string ClientDefinedIdentifier { get; set; }

        /// <summary>
        /// Generic List of objects that the client can pass-through with the request and be returned unaltered in the response.
        /// This can be a single word of information, number(s), a list of strings, or a complex object that the client wants to pass along with the request and be returned in the response. 
        /// </summary>
        public List<object> PassThroughItems { get; set; }

        /// <summary>
        /// Unique Id provided by LoanTek to track and lookup this response. Commonly referred to as the 'ResponseId'.
        /// </summary>
        public long LoanTekDefinedIdentifier { get; set; }

        /// <summary>
        /// Total time in Milliseconds it took to execute this request
        /// </summary>
        public double ExecutionTimeInMillisec { get; set; }

        /// <summary>
        /// The Api 'end-point' that executed this request. An end-point is defined as the namespace, controller, and http request method accessed. 
        /// </summary>
        public string ApiEndPoint { get; set; }

        /// <summary>
        /// If this response is from a cache, the ClientDefinedIdentifier / RequestId of the original request being used.  Will ONLY be non-null or have a value if a cache response is used.
        /// </summary>
        public string CachedId { get; set; }

        public string Message { get; set; }

        #endregion

        /// <summary>
        /// List of <see cref="com.LoanTek.Quoting.IQuote"/>IQuotes
        /// </summary>
        public List<IQuote> Quotes { get; set; }
    }
    
}