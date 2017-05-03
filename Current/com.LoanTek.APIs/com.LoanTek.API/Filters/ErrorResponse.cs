using System;
using System.Net;
using com.LoanTek.API.Filters;
using com.LoanTek.API.Requests;
using LoanTek.Utilities;

namespace com.LoanTek.API
{
    public class ErrorApiResponse : IApiResponse
    {
        public ErrorApiResponse()
        {
            this.EndTime = DateTime.Now;
        }
        public ErrorApiResponse(HttpStatusCode statusCode, string message, IApiResponse apiResponse)
        {
            ClassMappingUtilities.SetPropertiesForTarget(apiResponse, this);

            this.HttpStatusCode = statusCode;
            this.Message = message;
        }

        /// <summary>
        /// HttpStatusCode - this should be the same as the HttpResponse
        /// </summary>
        public HttpStatusCode HttpStatusCode { get; set; }

        public long Id { get; set; }
        public DateTime EndTime { get; set; }
        public double ExecutionTimeInMillisec { get; set; }

        /// <summary>
        /// This contains the error message
        /// </summary>
        public string Message { get; set; }
    }
}