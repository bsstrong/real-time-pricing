using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LoanTek.Utilities;
using System.Text;
using com.LoanTek.Master.Data.LinqDataContexts;
using Newtonsoft.Json;
using System.Web;
using System.ServiceModel.Channels;
using System.Configuration;
// ReSharper disable UnusedAutoPropertyAccessor.Local


namespace com.LoanTek.API.Pricing.Partners.Controllers
{
    [RoutePrefix("MBSHighway")]
    public class MbsHighwayController : ApiController
    {
        // POST: MBSHighway
        [HttpPost]
        [Route("UpdatedLenders")]

        public HttpResponseMessage Post()
        {
            string userIP = getClientIp();

            if (!checkIP(userIP))
                return new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("Unauthorized IP Address " + userIP) };

            String contentString = Request.Content.ReadAsStringAsync().Result;
            var req = JSON.SafelyParseJson<LenderRequest>(contentString);

            var startDate = DateTime.Parse(req.StartDate); // DateTime.Parse("01/01/2017");
            var endDate = DateTime.Parse(req.EndDate); // DateTime.Now;

            var dc = new QuoteSystemsDataContext();

            var updatedLenders = dc.tbl_RS_Masters.Where(m => m.LastUpdated > startDate && m.LastUpdated <= endDate).GroupBy(m => m.RSLenderID).Distinct().Count();
            var totaLenders = dc.tbl_RS_Masters.Where(m => m.Active == true).GroupBy(m => m.RSLenderID).Distinct().Count();

            var respObject = new LenderResponse { UpdatedLenders = updatedLenders, TotalLenders = totaLenders };
            
            var respJson = JsonConvert.SerializeObject(respObject);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(respJson, Encoding.UTF8, "application/json");
            return response;
        }

        private string getClientIp(HttpRequestMessage request = null)
        {
            request = request ?? Request;

            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }
            else if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }
            else
            {
                return "Cannot Determine";
            }
        }

        private bool checkIP(string userIP)
        {
            List<string> ipsFromWebConfig = new List<string>();
            string s = ConfigurationManager.AppSettings["AuthorizedMBSIPs"];
            if (!string.IsNullOrEmpty(s))
            {
                ipsFromWebConfig.AddRange(s.Split(','));
                for (int i = 0; i < ipsFromWebConfig.Count; i++)
                {
                    ipsFromWebConfig[i] = ipsFromWebConfig[i].Trim();
                }
            }

            ipsFromWebConfig.Add("10.0.1.*");        // boise internal network, for testing
            ipsFromWebConfig.Add("63.148.111.162");  // NEW boise office
            ipsFromWebConfig.Add("63.148.111.163");  // NEW boise office


            try
            {
                if (!string.IsNullOrEmpty(userIP))
                {
                    if (ipsFromWebConfig.Contains(userIP)) //if Ip is in the list then continue...
                    {
                        return true;
                    }
                    foreach (var item in ipsFromWebConfig.Where(x => x.EndsWith("*")).ToList())
                    {
                        if (userIP.StartsWith(item.Substring(0, item.IndexOf('*'))))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }


            return false;
        }

        private class LenderResponse
        {
            public int UpdatedLenders { get; set; }
            public int TotalLenders { get; set; }
        }

        private class LenderRequest
        {
            public string StartDate { get; set; }
            public string EndDate { get; set; }

        }

    }
}