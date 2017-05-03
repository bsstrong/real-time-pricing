using System;

namespace com.LoanTek.API.Requests
{
    public interface IApiRequest
    {
        long Id { get; set; }

        DateTime StartTime { get; set; }

        /// <summary>   
        /// This is a unique Id defined and sent by the Partner for this request.
        /// </summary>
        string ClientDefinedIdentifier { get; set; }

        string RemoteIP { get; set; }

        string LocalServerName { get; set; }

        string ApiName { get; set; }

        string ApiEndPoint { get; set; }

        string AuthToken { get; set; }

        string Url { get; set; }
    }
}
