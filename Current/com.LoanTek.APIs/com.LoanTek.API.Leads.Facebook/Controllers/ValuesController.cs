using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using com.LoanTek.API.Leads.Facebook.Models;
using com.LoanTek.Master.Data.LinqDataContexts;

namespace com.LoanTek.API.Leads.Facebook.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
            var newFBpage = new Integrations_FacebookPage
            {
                ClientID = 1

            };

            using (var dc = new IntegrationsDataContext())
            {
                dc.Integrations_FacebookPages.InsertOnSubmit(newFBpage);
                dc.SubmitChanges();
            }
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
