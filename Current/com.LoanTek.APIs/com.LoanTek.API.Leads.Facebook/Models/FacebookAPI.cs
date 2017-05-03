using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using com.LoanTek.Master.Data.LinqDataContexts;
using LoanTek.Utilities;
using System.Text.RegularExpressions;

namespace com.LoanTek.API.Leads.Facebook.Models
{
    public class FacebookAPI
    {
        private Integrations_FacebookApp myApp;

        private string adminEmail = "richard.feagan@loantek.com";

        string rootURL = "https://graph.facebook.com/v2.8/";

        public FacebookAPI(string applicaton="Leads")
        {
            using (var dc = new IntegrationsDataContext())
            {
                var rec = (from l in dc.Integrations_FacebookApps
                           where l.Application == applicaton
                           select l).FirstOrDefault();

                if (rec != null)
                {
                    myApp = rec;

                    //var newToken = UpdateToken(myApp.CurrentUserAccessToken);
                    //if (newToken != "")
                    //{
                    //    myApp.UserAccessTokenUpdated = DateTime.Now;
                    //    myApp.CurrentUserAccessToken = newToken;
                    //    dc.SubmitChanges();
                    //}
                }
                else
                {
                    Email.SendEmail(adminEmail, "Unable to retrieve Facebook Integration for Application=" + applicaton, "Exception being throw at "+DateTime.Now, true, string.Empty, string.Empty);
                    throw new Exception("Unable to retrieve Facebook Integration for Application=" + applicaton);
                }
            }
        }

        public FacebookLead GetLeadInfo(string leadGen_ID, string token="")
        {
            if (token == "")
                token = myApp.CurrentUserAccessToken;

            string postURL = rootURL + leadGen_ID + "?access_token=" + token;

            try
            {

                var request = WebRequest.Create(postURL) as HttpWebRequest;
                if (request != null)
                {
                    request.ContentType = "text/xml";
                    request.Method = "GET";

                    var response = request.GetResponse();
                    {
                        var responseStream = response.GetResponseStream();
                        if (responseStream != null)
                        {

                            var responseReader = new StreamReader(responseStream);
                            var responseContent = responseReader.ReadToEnd().Trim();

                            //Email.SendEmail(adminEmail, "FB Lead Return", "body:" + responseContent, true, string.Empty, string.Empty);
                            var rawLead = JSON.SafelyParseJson<FacebookRawLead>(responseContent);

                            var newLead = new FacebookLead();

                            newLead.Created = rawLead.created_time;
                            
                            for (int x=0;x<rawLead.field_data.Count;x++)
                            {
                                switch (rawLead.field_data[x].name.Trim().ToLower())
                                {
                                    case "email":
                                        newLead.Email = System.Text.RegularExpressions.Regex.Unescape(rawLead.field_data[x].values[0]);
                                        break;

                                    case "full_name":
                                        newLead.FullName = Regex.Unescape(rawLead.field_data[x].values[0]);
                                        break;

                                    case "city":
                                        newLead.City = Regex.Unescape(rawLead.field_data[x].values[0]);
                                        break;

                                    case "state":
                                        newLead.State = Regex.Unescape(rawLead.field_data[x].values[0]);
                                        break;

                                    case "zip_code":
                                        newLead.Zip = Regex.Unescape(rawLead.field_data[x].values[0]);
                                        break;
                                    default:
                                        Email.SendEmail(adminEmail, "FB Lead unknown field LeadGenID="+leadGen_ID, "Unknown field "+ rawLead.field_data[x].name+", value "+ rawLead.field_data[x].values[0]+"<br><br>"+responseContent, true, string.Empty, string.Empty);
                                        break;                                        
                                }                            
                            }

                            return newLead;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Email.SendEmail(adminEmail, "Lead Detail Exception", ex.ToString() + "<br><br>Stack Trace:" + ex.StackTrace, true, string.Empty, string.Empty);
            }
            return null;

        }

        public string UpdateToken(string oldToken)
        {
            string postURL = rootURL + "oauth/access_token?grant_type=fb_exchange_token&client_id="+myApp.AppID+"&client_secret="+myApp.AppSecret+"&fb_exchange_token="+oldToken;

            try
            {

                var request = WebRequest.Create(postURL) as HttpWebRequest;
                if (request != null)
                {
                    request.ContentType = "text/xml";
                    request.Method = "GET";

                    var response = request.GetResponse();
                    {
                        var responseStream = response.GetResponseStream();
                        if (responseStream != null)
                        {

                            var responseReader = new StreamReader(responseStream);
                            var responseContent = responseReader.ReadToEnd().Trim();

                            //Email.SendEmail(adminEmail, "Token Exchange", "body:" + responseContent, true, string.Empty, string.Empty);
                            var resp = JSON.SafelyParseJson<FacebookToken>(responseContent);
                            return resp.access_token;
                        }
                        else
                        {
                            return oldToken;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Email.SendEmail(adminEmail, "Token Exchange Exception", ex.ToString()+"<br><br>Stack Trace:"+ex.StackTrace, true, string.Empty, string.Empty);
            }
            return oldToken;
        }

        private class FacebookToken
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
        }

        private class FieldData
        {
            public string name { get; set; }
            public IList<string> values { get; set; }
        }

        private class FacebookRawLead
        {
            public DateTime created_time { get; set; }
            public string id { get; set; }
            public IList<FieldData> field_data { get; set; }
        }


    }
}