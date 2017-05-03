using System;
using System.Collections.Generic;
using com.LoanTek.CRM.Files;
using System.ComponentModel.DataAnnotations;
using System.Web.Http.Description;
using System.Xml.Serialization;
using com.LoanTek.API.Requests;
using Newtonsoft.Json;

namespace com.LoanTek.API.Leads.Clients.Models.Common
{
    public abstract class ALeadsRequest : Requests.IRequest
    {
        #region IRequest fields

        /// <summary>
        /// Generic List of objects that the client can pass-through with the request and be returned unaltered in the response.
        /// </summary>
        /// <remarks>
        /// This can be a single word of information, number(s), a list of strings, or a complex object that the client wants to pass along with the request and be returned in the response. 
        /// </remarks>
        public List<object> PassThroughItems { get; set; }

        #endregion

        [Required]
        public virtual AFile LeadFile { get; set; }

        public string ClientDefinedIdentifier { get; set; }
    }

}