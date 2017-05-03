using com.LoanTek.CRM.Files;

namespace com.LoanTek.API.Leads.Clients.Models.Common
{
    /// <summary>
    /// The Lead request that contains LeadFile.
    /// </summary>
    public class SalesLeadRequest : ALeadsRequest
    {
        /// <summary>
        /// Contains all the details for this lead. 
        /// </summary>
        public new SalesLeadFile LeadFile { get { return base.LeadFile as SalesLeadFile; } set { base.LeadFile = value; } }
    }

}