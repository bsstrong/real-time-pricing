using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using com.LoanTek.Quoting.Deposits.Common;

namespace com.LoanTek.API.Pricing.Clients.Models.Deposit
{
    /// <summary>
    /// A Client Deposit API Request is a pricing request for the API/Web Service(s) that contains a <see cref="DepositRequest"/> DepositRequest object. 
    /// </summary>
    public class DepositApiRequest : IClientRequest
    {
        #region IClientRequest fields

        /// <summary>
        /// UserId to use for this request. Must be a valid and active LoanTek UserId. 
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// True or False if you want detailed information on what Tier/Rate requirements failed.  
        /// This could allow you to build a stacktrace of why specific Rates were not returned. 
        /// The information is returned as a KeyValuePair 'PassThroughItem' with the key 'FailedRequirementRules'.
        /// </summary>
        /// <remarks>
        /// Setting this to true could result in a considerable amount of additional data being returned for every request. 
        /// Only set to true if you are debugging or need this information for a backend process. 
        /// Do not use for normal customer queries. 
        /// </remarks>
        public bool ReturnFailedRules { get; set; }

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

        #endregion

        #region IRequest fields

        /// <summary>
        /// The URL to post Quotes to. If null, then Quotes will be returned in the response. 
        /// </summary>
        /// <remarks>
        /// If PostbackInChuncks is set to true but this is null, then PostbackInChuncks is ignored.
        /// </remarks>
        //public string PostbackUrl { get; set; }

        /// <summary>
        /// True or False if the postback is to be split up into chunks. Each chunk should contain all the quotes for one Client. 
        /// Each request could have between 0 (no quotes to return) to X (one for each client that has quotes) number of postbacks. 
        /// If this is set to true, then PostbackUrl property MUST contain a valid URL to post the responses to. 
        /// </summary>
        /// <remarks>
        /// If this is set to true but PostbackUrl is null, then this is ignored.
        /// </remarks>
        //public bool PostbackInChunks { get; set; }

        #endregion

        /// <summary>   
        /// The <see cref="DepositRequest"/> DepositRequest contains all the deposit request details.
        /// </summary>  
        [Required]
        public DepositRequest DepositRequest { get; set; }

        /// <summary>   
        /// Request a copy of the Deposit Request be send back in the response.
        /// </summary>  
        public bool ReturnCopyOfDepositRequestInResponse { get; set; }

        /// <summary>
        /// A customized Json object representing only the data fields that you want to be returned for each quote in the response. 
        /// The base object used in the response is the <see cref="DepositQuote"/>DepositQuote object and therefore the field names in the customized Json object must match the field names of an <see cref="DepositQuote"/>DepositQuote object.
        /// </summary>
        public string CustomQuoteResponseJson { get; set; }

    }


}