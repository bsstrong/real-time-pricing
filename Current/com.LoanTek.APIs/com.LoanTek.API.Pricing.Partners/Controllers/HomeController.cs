using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Web.Http;
using System.Web.Http.Description;
using LoanTek.Utilities;

namespace com.LoanTek.API.Pricing.Partners.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : ApiController
    {
        
        [HttpGet]
        public HttpResponseMessage Index()
        {
            string msg = "<html><head><link href=\"https://fonts.googleapis.com/css?family=Roboto:400,700\" rel=\"stylesheet\"><style>body{padding:20px;font-family:'Roboto',sans-serif;}</style></head>";
            msg += "<body><h2>LoanTek Partners Pricing API</h2><br/>"+ 
                    "<p>" + ClientInfo.GetHostName() + ":" + ClientInfo.GetHostIpAddresses().First(n => n.AddressFamily == AddressFamily.InterNetwork) +"</p>";
            msg += "<br/><footer><p>&copy;" + DateTime.Now.Year + " - <a href=\"http://LoanTek.com\">LoanTek</a> | " +
                   "<a href=\"http://LoanTek.com/privacy-policy/\">Privacy Policy</a> | <a href=\"http://LoanTek.com/contact-us/\">Contact Us</a>" +
                   "</p><p><a href=\"tel:1-888-562-6835\">1-888-562-6835</a> | support@loantek.com</p></footer></body></html>";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(msg, Encoding.UTF8, "text/html") };
        }
    }
}
