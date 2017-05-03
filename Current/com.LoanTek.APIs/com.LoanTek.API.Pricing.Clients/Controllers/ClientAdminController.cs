using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using com.LoanTek.API.Common.Filters;
using LoanTek.Utilities;
using Cache = com.LoanTek.Quoting.Loans.Auto.Cache;

namespace com.LoanTek.API.Pricing.Clients.Controllers
{
    /// <summary>
    /// Controller for Admin functions
    /// </summary>
    [IPAddressValidation]
    [RoutePrefix("Admin")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ClientAdminController : ApiController
    {
        #region Default / Top-level Api

        /// <summary>
        /// Get the top-level API object for this controller
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Api")]
        //[EnableCors(origins: "*", headers: "*", methods: "*")]
        public Api GetApi()
        {

            var requestUri = this.Request.RequestUri;
            var api = Common.Global.ApiObject;
            api.Servers = new List<Server>()
            {
                new Server()
                {
                    ServerName = Common.Global.LocalServerName,
                    Domain = requestUri.Scheme + Uri.SchemeDelimiter + requestUri.Host + (requestUri.IsDefaultPort ? "" : ":" + requestUri.Port),
                    ServerStatus = Common.Global.ServerStatusType
                }
            };
            return api;
        }

        #endregion

        #region Reload Data

        [HttpGet]
        [Route("ReloadData")]
        public HttpResponseMessage ReloadData()
        {
            var context = new HttpContextWrapper(HttpContext.Current);
            var msg = ClientInfo.GetHostName() + " Manual ReloadData (" + this.Request.RequestUri + ") called by " + ClientInfo.GetIPAddress(context.Request);
            Debug.WriteLine(msg);
            //Email.EmailLoggingTech(msg, msg, "com.LoanTek.Module.Developers.UI.Controllers.HomeController.ReloadData");
            Email.SendEmail("eric@loantek.com", "support@loantek.com", msg, msg, true, null, null);
            Global.InitData();
            return new HttpResponseMessage(HttpStatusCode.OK) { ReasonPhrase = msg + " - Global.InitData Done!" };
        }

        #endregion

        #region Cache Manager

        [HttpGet]
        [Route("CacheManager/Deposits")]
        //[EnableCors(origins: "*", headers: "*", methods: "*")]
        public Quoting.Deposits.Common.Cache GetCacheManagerDeposits(bool clear = false)
        {
            if (clear)
            {
                Quoting.Deposits.Common.Cache.Instance.Clear();
            }
            return Quoting.Deposits.Common.Cache.Instance;
        }

        [HttpGet]
        [Route("CacheManager/Auto")]
        //[EnableCors(origins: "*", headers: "*", methods: "*")]
        public Cache GetCacheManagerAuto(bool clear = false)
        {
            if (clear)
            {
                Cache.Instance.Clear();
            }
            return Cache.Instance;
        }


        
        #endregion

    }
}
