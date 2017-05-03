using System.Net.Http;
using WebApiThrottle;

namespace com.LoanTek.API
{
    //https://github.com/stefanprodan/WebApiThrottle
    public class RateLimitHandler : ThrottlingHandler
    {
        protected override RequestIdentity SetIdentity(HttpRequestMessage request)
        {
            //Debug.WriteLine("SetIndentity authkey: " + request.RequestUri.Segments[1]);
            return new RequestIdentity()
            {
                ClientKey = (request.RequestUri.Segments.Length > 1) ? request.RequestUri.Segments[1] : "",
                ClientIp = base.GetClientIp(request).ToString(),
                Endpoint = request.RequestUri.AbsolutePath.ToLowerInvariant()
            };
        }
    }
}