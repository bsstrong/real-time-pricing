using System.Collections.Generic;
using com.LoanTek.API.Requests;

namespace com.LoanTek.API.Pricing.Partners.Models
{
    /// <summary>
    /// Interface for ALL Partner requests.
    /// </summary>
    public interface IPartnerRequest : Requests.IRequest
    {
        /// <summary>
        /// The URL to post Quotes to. If null, then Quotes will be returned in the response. 
        /// </summary>
        /// <remarks>
        /// If PostbackInchunks is set to true but this is null, then PostbackInchunks is ignored.
        /// </remarks>
        string PostbackUrl { get; set; }

        /// <summary>
        /// True or False if the postback is to be split up into chunks. Each chunk should contain all the quotes for one Client. 
        /// Each request could have between 0 (no quotes to return) to X (one for each client that has quotes) number of postbacks. 
        /// If this is set to true, then PostbackUrl property MUST contain a valid URL to post the responses to. 
        /// </summary>
        /// <remarks>
        /// If this is set to true but PostbackUrl is null, then this is ignored.
        /// </remarks>
        bool PostbackInChunks { get; set; }

        
    }
}