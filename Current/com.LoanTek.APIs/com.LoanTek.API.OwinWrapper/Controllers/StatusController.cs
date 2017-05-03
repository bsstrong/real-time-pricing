using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace com.LoanTek.API.OwinWrapper.Controllers
{
    [RoutePrefix("Status")]
    public class StatusController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage Index()
        {
            return About();
        }

        [Route("About")]
        [HttpPost]
        [HttpGet]
        public HttpResponseMessage About()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(DateTime.Now +" Welcome to the LoanTek API Owin Wrapper. Listening at " + Global.Config.Host + " for requests...\n\n" + Global.LoanTekArt4);
            return response;
        }
    }
}
