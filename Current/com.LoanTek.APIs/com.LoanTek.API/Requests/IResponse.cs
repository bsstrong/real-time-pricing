
namespace com.LoanTek.API.Requests
{
    /// <summary>
    /// Interface for ALL client responses.
    /// </summary>
    public interface IResponse : IRequest
    {
        /// <summary>
        /// Unique Id provided by LoanTek to track and lookup this response. Commonly referred to as the 'ResponseId'.
        /// </summary>
        string LoanTekDefinedIdentifier { get; set; }

        /// <summary>
        /// Total time in Milliseconds it took to execute this request
        /// </summary>
        double ExecutionTimeInMillisec { get; set; }

        /// <summary>
        /// This timestamp represents when this response was created. It is the total time in Milliseconds since the UTC 'Epoch' date time (01/01/1970).  
        /// </summary>
        long TimeStamp { get; set; }
            
        /// <summary>
        /// The Api 'end-point' that executed this request. An end-point is defined as the namespace, controller, and http request method accessed. 
        /// </summary>
        string ApiEndPoint { get; set; }

        /// <summary>
        /// If the response came from a cached resource, then this string hold the number of submissions cached out of the number of total submissions being returned. 
        /// </summary>
        string CachedId { get; set; }

        /// <summary>
        /// May hold additional information about the request or a possible error string if an exception was thrown while pricing the loan. 
        /// </summary>
        /// <remarks>
        /// Any error string this may contain is considered a non-fatal exception and the overall request can still have an Http Status Code 200 'OK'. 
        /// </remarks>
        string Message { get; set; }
    }
}