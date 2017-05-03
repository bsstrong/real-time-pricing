using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using com.LoanTek.API.Demo.Models.Bankrate.Mortgage;
using com.LoanTek.API.Demo.Models.Sdk;
using com.LoanTek.Forms.Mortgage;
using LoanTek.Utilities;
using Newtonsoft.Json;
using PreQualUsers = com.LoanTek.API.Demo.Models.Bankrate.Mortgage.PreQualUsers;
// ReSharper disable RedundantArgumentDefaultValue

namespace com.LoanTek.API.Demo.Controllers
{
    public class MortgageRequestController : Controller
    {
        // GET: MortgageRequest
        public ActionResult Index()
        {
            if (PreQualUsers.QuotingUsers == null)
            {
                Task.Run(() => PreQualUsers.UpdatePreQualUsers().ConfigureAwait(false));
            }
            IndexModel model = new IndexModel();
            return View(model);
        }

        public class IndexModel
        {
            public readonly SelectList ApiServers = new SelectList(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("http://localhost", "localhost"),
                new KeyValuePair<string, string>("http://10.83.95.25", "Century Link: 10.83.95.25"),
                new KeyValuePair<string, string>("http://sandbox-loantek-poc-lb-2022783474.us-east-1.elb.amazonaws.com", "AWS Load Balancer"),
                new KeyValuePair<string, string>("http://54.85.75.62", "AWS Single Server"),
            }, "key", "value");
            public string ApiServer { get; set; }
            public ConsumerMortgageLoanForm Form { get; set; } = new ConsumerMortgageLoanForm();
            public bool WaitUntilCompleted { get; set; }
        }

        #region v1
        //private CancellationTokenSource tokenSource = new CancellationTokenSource();
        [HttpPost]
        public async Task<ConcurrentBag<Model>> Quote(IndexModel model)
        {
            Debug.WriteLine("\n"+JsonConvert.SerializeObject(model));

            Connection.UpdateWebServiceBaseAddress(model.ApiServer);

            QuoteRequestWithJsonContent quoter = new QuoteRequestWithJsonContent();
            this.Session?.Add(typeof(QuoteRequestWithJsonContent).FullName, quoter);

            string clientDefinedIdPrefix = "BR-"+ StringUtilities.CreateRandomString(8);
        
            try
            {
                if (model.WaitUntilCompleted)
                {
                    return await quoter.Quote(model.Form, clientDefinedIdPrefix, 0, false);
                }
                Task.Run(() => quoter.Quote(model.Form, clientDefinedIdPrefix, 0, false)).ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return null;
        }

        public JsonResult GetResults()
        {
            try
            {
                QuoteRequestWithJsonContent quoter = this.Session[typeof(QuoteRequestWithJsonContent).FullName] as QuoteRequestWithJsonContent;
                if (quoter != null)
                    return Json(quoter.GetResult(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return null; // ?? new MortgageSubmission<Quote>() { ClientDefinedId = "Test" }
        }

        public string GetCounts()
        {
            try
            {
                QuoteRequestWithJsonContent quoter = this.Session[typeof(QuoteRequestWithJsonContent).FullName] as QuoteRequestWithJsonContent;
                if (quoter != null)
                {
                    return JsonConvert.SerializeObject(quoter, Global.JsonSettings);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return null;
        }

        #endregion

        #region v2

        [HttpPost]
        public async Task<ConcurrentBag<string>> Quote2(IndexModel model)
        {
            Connection.UpdateWebServiceBaseAddress(model.ApiServer);

            QuoteRequestWithStringContent quoter = new QuoteRequestWithStringContent();
            this.Session?.Add(typeof(QuoteRequestWithStringContent).FullName, quoter);

            string clientDefinedIdPrefix = "BR-" + StringUtilities.CreateRandomString(8);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                if (model.WaitUntilCompleted)
                {
                    return await quoter.Quote(model.Form, clientDefinedIdPrefix, 0, false);
                }
                Task.Run(() => quoter.Quote(model.Form, clientDefinedIdPrefix, 0, false)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            sw.Stop();
            Debug.WriteLine("total time to quote2:" + sw.ElapsedMilliseconds);
            return null;
        }

        public string GetResults2()
        {
            try
            {
                QuoteRequestWithStringContent quoter = this.Session[typeof(QuoteRequestWithStringContent).FullName] as QuoteRequestWithStringContent;
                if (quoter != null)
                    return quoter.GetResult();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return null;
        }

        public string GetCounts2()
        {
            try
            {
                QuoteRequestWithStringContent quoter = this.Session[typeof(QuoteRequestWithStringContent).FullName] as QuoteRequestWithStringContent;
                if (quoter != null)
                {
                    return JsonConvert.SerializeObject(quoter, Global.JsonSettings);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return null;
        }

        #endregion

        #region v3

        [HttpPost]
        public async Task<ConcurrentBag<Model>> Quote3(IndexModel model)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Connection.UpdateWebServiceBaseAddress(model.ApiServer);

            QuoteRequestStringJsonHybrid quoter = new QuoteRequestStringJsonHybrid();
            this.Session?.Add(typeof(QuoteRequestStringJsonHybrid).FullName, quoter);

            string clientDefinedIdPrefix = "BR-" + StringUtilities.CreateRandomString(8);

            try
            {
                if (model.WaitUntilCompleted)
                {
                    return await quoter.Quote(model.Form, clientDefinedIdPrefix, 0, false, sw);
                }
                Task.Run(() => quoter.Quote(model.Form, clientDefinedIdPrefix, 0, false, sw)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return null;
        }

        public JsonResult GetResults3()
        {
            try
            {
                QuoteRequestStringJsonHybrid quoter = this.Session[typeof(QuoteRequestStringJsonHybrid).FullName] as QuoteRequestStringJsonHybrid;
                if (quoter != null)
                    return Json(quoter.GetResult(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return null; 
        }

        public string GetCounts3()
        {
            try
            {
                QuoteRequestStringJsonHybrid quoter = this.Session[typeof(QuoteRequestStringJsonHybrid).FullName] as QuoteRequestStringJsonHybrid;
                if (quoter != null)
                {
                    return JsonConvert.SerializeObject(quoter, Global.JsonSettings);
                }
            }
            catch (Exception e) 
            {
                Debug.WriteLine(e);
            }
            return null;
        }

        #endregion

    }
}
