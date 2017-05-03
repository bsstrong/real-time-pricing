using System;
using System.Net;

namespace com.LoanTek.API.Requests
{
    public interface IApiResponse
    {
        long Id { get; set; }
        DateTime EndTime { get; set; }
        double ExecutionTimeInMillisec { get; set; }
        string Message { get; set; }
        HttpStatusCode HttpStatusCode { get; set; }
    }
}
