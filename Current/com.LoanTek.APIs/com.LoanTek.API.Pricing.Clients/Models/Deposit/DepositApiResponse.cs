using System.Collections.Generic;
using com.LoanTek.Quoting.Deposits.Common;
using com.LoanTek.Types;

namespace com.LoanTek.API.Pricing.Clients.Models.Deposit
{

    /// <summary>
    /// Returns a List of <see cref="DepositSubmission"/>DepositSubmission objects based on the data points of the passed in <see cref="DepositRequest"/> DepositRequest object. 
    /// </summary>
    public class DepositApiResponse : IClientResponse
    {
        #region interface fields

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
        /// If the response came from a cached resource, then this string hold the number of submissions cached out of the number of total submissions being returned. 
        /// </summary>
        public string CachedId { get; set; }

        /// <summary>
        /// May hold additional information about the request or a possible error string if an exception was thrown while pricing the loan. 
        /// </summary>
        /// <remarks>
        /// Any error string this may contain is considered a non-fatal exception and the overall request can still have an Http Status Code 200 'OK'. 
        /// </remarks>
        public string Message { get; set; }

        /// <summary>
        /// Generic List of KeyValuePair objects that may be returned as part of the response. 
        /// Usually containing additional data, such as failed or passed rules, on why certain Rate Sheet Tiers and/or specific Rates were or were not returned. 
        /// </summary>
        /// <remarks>
        /// Options in the Request, such as setting the 'ReturnFailedRequirementRules' boolean to 'true', is what would cause this property to be populated with data. 
        /// </remarks>
        public List<KeyValuePair<string, object>> AdditionalItems { get; set; }

        #endregion

        /// <summary>
        /// The <see cref="Processing.StatusType"/> Status of this request.
        /// </summary>
        /// <remarks>
        /// If the Status is 'Cancelled' then the request failed to finish processing in the allotted time.
        /// </remarks>
        public Processing.StatusType Status { get; set; }

        /// <summary>
        /// List of <see cref="DepositSubmission"/>DepositSubmission. Each DepositSubmission (a submission) contains the quotes for one user, the user's quoting information, and the request id (ClientDefinedIdentifier).
        /// </summary>
        /// <remarks>
        /// The collection of quotes being returned are Deposit objects by default, but the properties returned can be limited using the ClientDepositRequest.CustomQuoteResponseJson property.
        /// </remarks>
        public List<DepositSubmission> Submissions { get; set; }

        /// <summary>   
        /// A copy of the <see cref="DepositRequest"/> DepositRequest sent to and used by this web service.
        /// </summary>  
        /// <remarks>
        /// This value will be null unless the property 'ReturnCopyOfDepositRequestInResponse' is set to true in the DepositApiRequest.
        /// </remarks>
        public DepositRequest DepositRequest { get; set; }
    }
    
}