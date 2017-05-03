using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using LoanTek.Utilities;
using Nancy;
using Nancy.Facebook.RealtimeSubscription;
using HttpStatusCode = System.Net.HttpStatusCode;
using com.LoanTek.API.Leads.Facebook.Models;
using com.LoanTek.Master.Data.LinqDataContexts;
using LoanTek.BusinessObjects.Leads;
using System.Web;
using System.ServiceModel.Channels;
using Newtonsoft.Json;

namespace com.LoanTek.API.Leads.Facebook.Controllers
{
    /// <summary>
    /// Webservice for Facebook Webhooks
    /// </summary>

    [RoutePrefix("api/webhooks")]
    public class WebhooksController : Wrapper
    {
        
        const string SecurityToken = "vHcxnc6RYckBKYWJSKX4Ve6P8Q6RDrveRLMbvYM9R3kGgRwWwM";

        //[Route("{encryptedClientId}")]
        [Route("")]
        [HttpGet]
        public HttpResponseMessage Get()  //string encryptedClientId
        {
            GetParams(Request);
            var hubVerifyToken = GetParam("hub.verify_token");

            Email.SendEmail("richard.feagan@loantek.com", "GET on FB API", "VT=" + NullSafe.NullSafeString(hubVerifyToken) + ", HC=" + NullSafe.NullSafeString(GetParam("hub.challenge")), true, string.Empty, string.Empty);

            if (!SecurityToken.Equals(hubVerifyToken))
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Invalid Verify Token");

            var hubChallenge = GetParam("hub.challenge");
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(hubChallenge, Encoding.UTF8, "application/json");
            return response;
        }

        // [Route("{encryptedClientId}")]
        [Route("")]
        [HttpPost]
        public HttpResponseMessage Post()  //, [FromBody] string changes), string encryptedClientId
        {
            String contentString = Request.Content.ReadAsStringAsync().Result.ToString();  
            ChangesModel req;
            string leadGenID="";
            string pageID = "";
            int lgid=0;
            int clientID = -1;
            string token = "";

            var clientIP = GetClientIp(); 

            #region Handle Request
            try
            {

                req = JSON.SafelyParseJson<ChangesModel>(contentString);

                if (req.entry[0].changes[0].field != "leadgen")
                {
                    // This is NOT a Leadgen event, log it?  
                    Email.SendEmail("richard.feagan@loantek.com", "POST on FB API - non-leadgen", "body:" + contentString + " ; field=" + req.entry[0].changes[0].field, true, string.Empty, string.Empty);
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                leadGenID = req.entry[0].changes[0].value.leadgen_id;
                pageID = req.entry[0].changes[0].value.page_id;
            }
            catch (Exception ex)
            {
                // Log it??
                Email.SendEmail("richard.feagan@loantek.com", "POST EXCEPTION on FB API", "body:" + contentString + " ; LeadGenID=" + leadGenID + " ; PageID = " + pageID, true, string.Empty, string.Empty);
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
            #endregion

            var fb = new FacebookAPI("Leads");

            #region Database calls
            using (var dc = new IntegrationsDataContext())
            {
                var page = (from p in dc.Integrations_FacebookPages
                            where p.PageID == pageID && p.AccessRevoked == null
                           select p).FirstOrDefault();

                if (page != null)
                {
                        clientID = (int)page.ClientID;
                }
                else
                {
                    Email.SendEmail("richard.feagan@loantek.com", "Unable to retrieve Facebook Integration for Page=" + pageID, "Invalid Request at " + DateTime.Now, true, string.Empty, string.Empty);
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                page.PageAccessToken = fb.UpdateToken(page.PageAccessToken);
                token = page.PageAccessToken;

                var newLeadGen = new Integrations_FacebookLeadGen
                {
                    ClientID = clientID,
                    LeadGen_ID = leadGenID,
                    PageID = pageID,
                    Posted = DateTime.Now,
                    PostJSON = contentString,
                    RequestIP = clientIP
                };

                dc.Integrations_FacebookLeadGens.InsertOnSubmit(newLeadGen);
                dc.SubmitChanges();
                lgid = newLeadGen.ID;
            }
            #endregion

            #region create LoanTek lead

            var myLead = fb.GetLeadInfo(leadGenID, token);

            var loanTekLead = myLead.ToMortgageLead(clientID, true);

            if (loanTekLead != null)
            {
                var newFBLead = new Integrations_FacebookLead
                {
                    LeadGenID = lgid,
                    LeadID = loanTekLead.LeadId,
                    LeadJSON = contentString
                };

                using (var dc = new IntegrationsDataContext())
                {
                    dc.Integrations_FacebookLeads.InsertOnSubmit(newFBLead);
                    dc.SubmitChanges();
                }
                Email.SendEmail("richard.feagan@loantek.com", "FB API Lead Post Success", "Integrations_FacebookLeads.ID="+newFBLead.ID.ToString()+"  tbl_Leads.LeadID="+ loanTekLead.LeadId.ToString()+ "LeadGenID=" + leadGenID + " ; PageID = " + pageID+"<br><br>Body:<br>"+contentString, true, string.Empty, string.Empty);
            }
            else
            {
                Email.SendEmail("richard.feagan@loantek.com", "FB Leads - LoanTek Lead creation failure", "body:" + contentString + " ; LeadGenID="+ leadGenID+" ; PageID = "+pageID, true, string.Empty, string.Empty);
            }
            #endregion

            return Request.CreateResponse(HttpStatusCode.OK, contentString);
        }

        [Route("{encryptedClientId}")]
        [HttpPost]
        public HttpResponseMessage Post(string encryptedClientId)  
        {
            GetParams(Request);

            #region Add Page
            if (GetParam("Action").ToUpper() == "ADD")
            {
                using (var dc = new IntegrationsDataContext())
                {
                    int appID = (from l in dc.Integrations_FacebookApps
                                 where l.Application == "Leads"
                                 select l).FirstOrDefault().ID;

                    int clientID = int.Parse(Encryption.DecryptString(encryptedClientId));
                    if (!(clientID > 0))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid ClientID");
                    }

                    var findPage = (from p in dc.Integrations_FacebookPages where p.ClientID == clientID && p.PageID == GetParam("PageID") && p.AccessRevoked == null select p).FirstOrDefault();

                    if (findPage != null)
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Already Subscribed");

                    var fb = new FacebookAPI("Leads");

                    var newPage = new Integrations_FacebookPage
                    {
                        ClientID = int.Parse(Encryption.DecryptString(encryptedClientId)),
                        AppID = appID,
                        PageAccessToken = fb.UpdateToken(GetParam("Token")),
                        PageName = GetParam("PageName"),
                        PageID = GetParam("PageID"),
                        AccessGranted = DateTime.Now
                    };

                    dc.Integrations_FacebookPages.InsertOnSubmit(newPage);
                    dc.SubmitChanges();
                }
                return Request.CreateResponse(HttpStatusCode.OK, "Success");
            }
            #endregion
            #region Remove Page
            else if (GetParam("Action").ToUpper() == "REMOVE")
            {
                using (var dc = new IntegrationsDataContext())
                {

                    int appID = (from l in dc.Integrations_FacebookApps
                                 where l.Application == "Leads"
                                 select l).FirstOrDefault().ID;

                    int clientID = int.Parse(Encryption.DecryptString(encryptedClientId));
                    if (!(clientID > 0))
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid ClientID");
                    var removePage = (from p in dc.Integrations_FacebookPages where p.ClientID == clientID && p.PageID == GetParam("PageID") && p.AccessRevoked == null select p).FirstOrDefault();

                    if (removePage == null)
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid PageID");

                    removePage.AccessRevoked = DateTime.Now;
                    dc.SubmitChanges();
                }

                return Request.CreateResponse(HttpStatusCode.OK, "Success");
            }
            #endregion
            return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid Request Type");
        }

        private class PageInfo
        {
            public string PageName { get; set; }
            public string PageID { get; set; }
            public string UserID { get; set; }

            public PageInfo(Integrations_FacebookPage source)
            {
                PageName = source.PageName;
                PageID = source.PageID;
                UserID = source.UserID;
            }
        }

        [Route("{encryptedClientId}")]
        [HttpGet]
        public HttpResponseMessage Get(string encryptedClientId)
        {
            GetParams(Request);

            string pagesJSON = "";

            using (var dc = new IntegrationsDataContext())
            {
                var sendPages = new List<PageInfo>();
                int appID = (from l in dc.Integrations_FacebookApps
                             where l.Application == "Leads"
                             select l).FirstOrDefault().ID;

                int clientID = int.Parse(Encryption.DecryptString(encryptedClientId));
                if (!(clientID > 0))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid ClientID");
                var userPages = (from p in dc.Integrations_FacebookPages where p.ClientID == clientID && p.AccessRevoked==null select p).ToArray();
                foreach (var addPage in userPages)
                {
                    sendPages.Add(new PageInfo(addPage));
                }
                
                pagesJSON = JsonConvert.SerializeObject(sendPages);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(pagesJSON, Encoding.UTF8, "application/json");
            return response;
        }


        private string GetClientIp(HttpRequestMessage request = null)
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
    }
}
