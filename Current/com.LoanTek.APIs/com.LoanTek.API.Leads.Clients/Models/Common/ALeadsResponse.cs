using System.Collections.Generic;
using com.LoanTek.API.Requests;
using com.LoanTek.CRM.Processing;

namespace com.LoanTek.API.Leads.Clients.Models.Common
{
    public class ALeadsResponse : Requests.IResponse
    {
        #region IResponse fields

        public string ClientDefinedIdentifier { get; set; }
        public List<object> PassThroughItems { get; set; }
        public string LoanTekDefinedIdentifier { get; set; }
        public double ExecutionTimeInMillisec { get; set; }
        public long TimeStamp { get; set; }
        public string ApiEndPoint { get; set; }
        public string CachedId { get; set; }
        public string Message { get; set; }

        #endregion

        public StatusType Status { get; set; }
    }
}