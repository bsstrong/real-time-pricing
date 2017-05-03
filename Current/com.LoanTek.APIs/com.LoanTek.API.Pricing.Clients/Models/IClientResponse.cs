using System.Collections.Generic;

namespace com.LoanTek.API.Pricing.Clients.Models
{
    /// <summary>
    /// Interface for ALL Partner responses.
    /// </summary>
    public interface IClientResponse : Requests.IResponse
    {
        /// <summary>
        /// Generic List of KeyValuePair objects that may be returned as part of the response. 
        /// Usually containing additional data, such as failed or passed rules, on why certain Rate Sheet Tiers and/or specific Rates were or were not returned. 
        /// </summary>
        List<KeyValuePair<string, object>> AdditionalItems { get; set; }
    }
}