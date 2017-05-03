using System;
using System.Web.Http.Description;
using com.LoanTek.API.Requests;
using Newtonsoft.Json;

namespace com.LoanTek.API.Pricing.Clients.Models.Mortgage
{
    /// <summary>
    /// A Mortgage Request contains a <see cref="MortgageLoanRequest"/> MortgageLoanRequest object. 
    /// </summary>
    public class MortgageApiRequest : Partners.Models.Common.FullMortgageRequest
    {
        /// <summary>
        /// UserId to use for this request. Must be a valid and active LoanTek UserId. 
        /// </summary>
        public int UserId { get; set; }

        #region Partners.Models.Common.FullMortgageRequest fields

        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        [JsonIgnore]
        public new string PostbackUrl { get; set; }

        [Obsolete]
        [ApiExplorerSettings(IgnoreApi = true)]
        [JsonIgnore]
        public new bool PostbackInChunks { get; set; }

        #endregion

    }

}