using System.Collections.Generic;
using com.LoanTek.Quoting.Mortgage;
using com.LoanTek.Quoting.Mortgage.Common;
using LoanTek.Pricing.LoanRequests;
using LoanTek.Utilities;


namespace com.LoanTek.API.Pricing.Partners.Models.Mortgage
{
    /// <summary>
    /// Retures a List of <see cref="MortgageQuote"/>MortgageQuote objects based on the data points of the passed in <see cref="LoanPricerLoanRequest"/> LoanPricerLoanRequest object. 
    /// </summary>
    public class FullMortgageResponse<TSubmission, TQuote> : MortgageResponse<TSubmission, TQuote>, IResponse where TSubmission : IMortgageSubmission<TQuote>
    {
        #region IResponse fields

        /// <summary>
        /// Generic List of objects that the client can pass-through with the request and be returned unaltered in the response.
        /// This can be a single word of information, number(s), a list of strings, or a complex object that the client wants to pass along with the request and be returned in the response. 
        /// </summary>
        public List<object> PassThroughItems { get; set; }

        /// <summary>
        /// This timestamp represents when this response was created. It is the total time in Milliseconds since the UTC 'Epoch' date time (01/01/1970).  
        /// </summary>
        public long TimeStamp { get; set; }

        /// <summary>
        /// The Api 'end-point' that executed this request. An end-point is defined as the namespace, controller, and http request method accessed. 
        /// </summary>
        public string ApiEndPoint { get; set; }

        #endregion

        public List<DateAndTime.TimeObject2> RequestTimeObjects { get; set; }
        public List<DateAndTime.TimeObject2> SubmissionTimeObjects { get; set; }
    }
    
}