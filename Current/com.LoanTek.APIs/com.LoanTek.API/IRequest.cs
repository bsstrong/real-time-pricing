using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace com.LoanTek.API
{
    /// <summary>
    /// Interface for ALL client requests.
    /// </summary>
    public interface IRequest
    {
        /// <summary>
        /// Unique Id provided by the client to track and lookup this request. Commonly referred to as the 'RequestId'.
        /// </summary>
        [Required]
        [MinLength(8)]
        [MaxLength(40)]
        string ClientDefinedId { get; set; }

        /// <summary>   
        /// Generic List of objects that the client can pass-through with the request and be returned unaltered in the response.
        /// This can be a single word of information, number(s), a list of strings, or a complex object that the client wants to pass along with the request and be returned in the response. 
        /// </summary>
        List<object> PassThroughItems { get; set; }
    }
}