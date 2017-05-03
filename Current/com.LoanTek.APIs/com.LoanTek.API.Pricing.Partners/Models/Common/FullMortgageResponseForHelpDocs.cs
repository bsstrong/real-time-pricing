using System.Collections.Generic;
using com.LoanTek.Quoting;
using com.LoanTek.Types;
using LoanTek.Pricing.LoanRequests;

//using com.LoanTek.API.Pricing.Mortgage.Areas.HelpPage.ModelDescriptions;

namespace com.LoanTek.API.Pricing.Partners.Models.Common
{

    /// <summary>
    /// Retures a List of <see cref="com.LoanTek.Quoting.IQuote"/>IQuote objects based on the data points of the passed in <see cref="LoanPricerLoanRequest"/> LoanPricerLoanRequest object. 
    /// </summary>
    public class FullMortgageResponse<T> : IPartnerResponse
    {
        //public FullMortgageResponse() {}

        //public FullMortgageResponse(List<com.LoanTek.Quoting.Bankrate.Converter.LoanQuoteSubmission> submissions)
        //{
        //    this.Submissions = submissions;
        //}

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
        /// The URL to post Quotes to. If null, then Quotes will be returned in the response. 
        /// </summary>
        /// <remarks>
        /// If PostbackInchunks is set to true but this is null, then PostbackInchunks is ignored.
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
        public bool PostbackInchunks { get; set; }
        
        /// <summary>
        /// Unique Id provided by LoanTek to track and lookup this response. Commonly referred to as the 'ResponseId'.
        /// </summary>
        public string LoanTekDefinedIdentifier { get; set; }

        /// <summary>
        /// Total time in Milliseconds it took to execute this request
        /// </summary>
        public double ExecutionTimeInMillisec { get; set; }

        /// <summary>
        /// This timestamp represents when this response was created. It is the total time in Milliseconds since the UTC 'Epoch' date time (01/01/1970).  
        /// </summary>
        public long TimeStamp { get; set; }

        /// <summary>
        /// The Api 'end-point' that executed this request. An end-point is defined as the namespace, controller, and http request method accessed. 
        /// </summary>
        public string ApiEndPoint { get; set; }

        /// <summary>
        /// If any block of user quotes (a submission) are from a cache, then the number of cached submissions is returned. 
        /// Format is cached# / total#, example: 1/3, meaning 1 out of the 3 submissions was cached.
        /// </summary>
        public string CachedId { get; set; }

        /// <summary>
        /// May hold additional information about the request or a possible error string if an exception was thrown while pricing the loan. 
        /// </summary>
        /// <remarks>
        /// Any error string this may contain is considered a non-fatal exception and the overall request can still have an Http Status Code 200 'OK'. 
        /// </remarks>
        public string Message { get; set; }

        #endregion

        /// <summary>
        /// The <see cref="Processing.StatusType"/> Status of this request.
        /// </summary>
        /// <remarks>
        /// If the Status is 'Cancelled' then the request failed to finish processing in the allotted time.
        /// </remarks>
        public Processing.StatusType Status { get; set; }

        /// <summary>
        /// List of <see cref="ISubmission"/>IQuoteSubmissions. Each IQuoteSubmission (a submission) contains the quotes for one user, the user's quoting information, and the request id (ClientDefinedIdentifier).
        /// </summary>
        /// <remarks>
        /// The collection of quotes being returned are IQuote objects by default, but the properties returned can be limited using the FullMortgageRequest.CustomQuoteResponseJson property.
        /// </remarks>
        public List<T> Submissions { get; set; }
    }
    
}