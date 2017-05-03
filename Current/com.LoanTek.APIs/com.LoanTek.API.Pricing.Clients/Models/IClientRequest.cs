
namespace com.LoanTek.API.Pricing.Clients.Models
{
    /// <summary>
    /// Interface for ALL Partner requests.
    /// </summary>
    public interface IClientRequest : Requests.IRequest
    {
        /// <summary>
        /// UserId to use for this request. Must be a valid and active LoanTek UserId. 
        /// </summary>
        int UserId { get; set; }

        /// <summary>
        /// True or False if you want detailed information on what Tier/Rate/Adjustment/Cap/ClientFee requirements failed.  
        /// This could allow you to build a stacktrace of why specific Rates were not returned and why the APR / Rate is what it is.  
        /// The information is returned as a KeyValuePair 'PassThroughItem' with the keys 'FailedRequirementRules', 'FailedAdjustmentRules', 'FailedCapRules', and 'FailedClientFeeRules'.
        /// </summary>
        /// <remarks>
        /// Setting this to true could result in a considerable amount of additional data being returned for every request. 
        /// Only set to true if you are debugging or need this information for a backend process. 
        /// Do not use for normal customer queries. 
        /// </remarks>
        bool ReturnFailedRules { get; set; }

        ///// <summary>
        ///// The URL to post Quotes to. If null, then Quotes will be returned in the response. 
        ///// </summary>
        ///// <remarks>
        ///// If PostbackInChunks is set to true but this is null, then PostbackInChunks is ignored.
        ///// </remarks>
        //string PostbackUrl { get; set; }

        ///// <summary>
        ///// True or False if the postback is to be split up into chunks. Each chunk should contain all the quotes for one Client. 
        ///// Each request could have between 0 (no quotes to return) to X (one for each client that has quotes) number of postbacks. 
        ///// If this is set to true, then PostbackUrl property MUST contain a valid URL to post the responses to. 
        ///// </summary>
        ///// <remarks>
        ///// If this is set to true but PostbackUrl is null, then this is ignored.
        ///// </remarks>
        //bool PostbackInChunks { get; set; }


    }
}