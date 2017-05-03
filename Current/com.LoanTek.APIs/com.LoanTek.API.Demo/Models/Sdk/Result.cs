using System.Net;

namespace com.LoanTek.API.Demo.Models.Sdk
{
    public class Result<TResponseContent>
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        public bool IsSuccessStatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public virtual TResponseContent Content { get; set; }
        public double TimeInMillisecondsToProcess { get; set; }
    }
}