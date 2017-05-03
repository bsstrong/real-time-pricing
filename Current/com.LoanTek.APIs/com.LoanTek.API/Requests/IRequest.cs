using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Http.Description;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace com.LoanTek.API.Requests
{
    /// <summary>
    /// Interface for ALL client requests.
    /// </summary>
    public interface IRequest
    {
        /// <summary>
        /// Use ClientDefinedId
        /// </summary>
        //[Obsolete]
        //[ApiExplorerSettings(IgnoreApi = true)]
        //[JsonIgnore]
        //[XmlIgnore]
        [MinLength(8)]
        [MaxLength(40)]
        [Required]
        string ClientDefinedIdentifier { get; set; }

        ///// <summary>
        ///// Used to hold a unique id created by the client for this request. Should be able to reference this request from the client using this id.
        ///// </summary>
        ///// <remarks>
        ///// Required
        ///// </remarks>
        ////[Required]
        //[MinLength(8)]
        //[MaxLength(40)]
        //string ClientDefinedId { get; set; }

        /// <summary>
        /// Generic List of objects that the client can pass-through with the request and be returned unaltered in the response.
        /// This can be a single word of information, number(s), a list of strings, or a complex object that the client wants to pass along with the request and be returned in the response. 
        /// </summary>
        List<object> PassThroughItems { get; set; }
    }
}