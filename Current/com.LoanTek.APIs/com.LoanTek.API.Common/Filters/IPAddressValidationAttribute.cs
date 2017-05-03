using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using LoanTek.Utilities;

namespace com.LoanTek.API.Common.Filters
{
    /// <summary>
    /// Only allows authorized IP addresses access. 
    /// </summary>
    public class IPAddressValidationAttribute : ActionFilterAttribute
    {
        public AuthorizedIPRepository AuthorizedList = new AuthorizedIPRepository();

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var context = actionContext.Request.Properties["MS_HttpContext"] as HttpContextBase;
            if (context != null)
            {
                var userIP = context.Request.UserHostAddress;
                try
                {
                    if (!string.IsNullOrEmpty(userIP))
                    {
                        if (AuthorizedList.GetAuthorizedIPs().Contains(userIP)) //if Ip is in the list then continue...
                        {
                            return;
                        }
                        foreach (var item in AuthorizedList.GetAuthorizedIPs().Where(x => x.EndsWith("*")).ToList())
                        {
                            if (userIP.StartsWith(item.Substring(0, item.IndexOf('*'))))
                            {
                                return;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                string url = string.Empty;
                try
                {
                    if (actionContext.Request != null && actionContext.Request.RequestUri != null)
                        url = actionContext.Request.RequestUri.Host + " " + actionContext.Request.RequestUri.PathAndQuery;
                }
                catch (Exception ex)
                {
                    url = "Exception:" + ex.Message;
                }

                Email.SendEmail("Logging.Tech@loantek.com", "IP Check Failed", $" IP: {userIP},<br /> Authorized IPs: {string.Join("|", this.AuthorizedList.GetAuthorizedIPs())},<br /> Webservice: {url}", true, string.Empty, string.Empty);

                actionContext.Response = new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("Unauthorized IP Address " + userIP) };
            }
        }
    }

    /// <summary>
    /// Holds the Authorized IP Addresses
    /// </summary>
    public class AuthorizedIPRepository
    {
        private static List<string> ipsFromWebConfig;

        private void setIPList()
        {
            ipsFromWebConfig = new List<string>();
            string s = ConfigurationManager.AppSettings["AuthorizedIPs"];
            if (!string.IsNullOrEmpty(s))
            {
                ipsFromWebConfig.AddRange(s.Split(','));
                for (int i = 0; i < ipsFromWebConfig.Count; i++)
                {
                    ipsFromWebConfig[i] = ipsFromWebConfig[i].Trim();
                }
            }
        }

        private List<string> ips;

        public List<string> GetAuthorizedIPs()
        {
            if (ips?.Count > 0)
                return ips;

            if (ipsFromWebConfig == null)
                this.setIPList();

            ips = new List<string>();

            if (ipsFromWebConfig != null)
                ips.AddRange(ipsFromWebConfig);

            ips.Add("127.0.0.1"); // IPV4
            ips.Add("::1"); // IPV6 loopback
            ips.Add("63.131.228.59"); // sql01
            ips.Add("63.131.228.60"); // dev01
            ips.Add("63.131.228.61"); // web01
            ips.Add("63.131.228.62"); // web02
            ips.Add("10.0.1.*"); // boise internal network
            ips.Add("10.133.133.*"); // Datacenter network
            ips.Add("63.148.111.162"); // NEW boise office
            ips.Add("63.148.111.163"); // NEW boise office
            return ips;
        }
    }

}