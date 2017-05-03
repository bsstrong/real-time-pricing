using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using com.LoanTek.API.Common.Filters;
using com.LoanTek.API.Instances;
using com.LoanTek.Types;
using LoanTek.Utilities;
using Zillow = com.LoanTek.Quoting.Zillow;

namespace com.LoanTek.API.Pricing.Partners.Controllers
{
    [IPAddressValidation]
    [ApiExplorerSettings(IgnoreApi = true)]
    [RoutePrefix("Admin")]
    public class AdminController : ApiController
    {
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
            return new HttpResponseMessage(HttpStatusCode.OK) {ReasonPhrase = msg + " - Global.InitData Done!"};
        }

        #region Default / Top-level Api

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


        /*
        #region Cache

        #region Common

        [HttpGet]
        [Route("CacheInstance/{quotingChannelType}")]
        public CacheInstance GetCacheCommon(QuotingChannelType quotingChannelType)
        {
            switch (quotingChannelType)
            {
                //case QuotingChannelType.NotChannelSpecific: return new CacheInstance(quotingChannelType, Quoting.Common.Cache.Instance);
                //case QuotingChannelType.Zillow: return new CacheInstance(quotingChannelType, Zillow.Cache.Instance);
                //case QuotingChannelType.Bankrate: return new CacheInstance(quotingChannelType, Quoting.Common.Cache.Instance);
                //case QuotingChannelType.LoanTek: return new CacheInstance(quotingChannelType, Quoting.Common.Cache.Instance);
                default: throw new ArgumentOutOfRangeException(nameof(quotingChannelType), quotingChannelType, null);
            }
            return new CacheInstance();
        }

        #endregion

        #region Zillow

        //[HttpPost]
        //[Route("CacheManager/Zillow")]
        //public ApiCacheManager.CacheManager UpdateCacheManagerZillow(ApiCacheManager.CacheManager cacheManager)
        //{
        //    Zillow.Cache cache = Zillow.Cache.Instance;

        //    //require minimums
        //    if (cacheManager?.ExpirationModeType != null)
        //    {
        //        cache.ExpirationModeType = cacheManager.ExpirationModeType.Value;
        //    }
        //    if (cacheManager?.ExpirationTime != null)
        //    {
        //        if (cacheManager.ExpirationTime.GetValueOrDefault().TotalSeconds < 60)
        //            cacheManager.ExpirationTime = TimeSpan.FromSeconds(60);
        //        cache.ExpirationTime = cacheManager.ExpirationTime.Value;
        //    }
        //    if (cacheManager?.InvalidateCheckInterval != null)
        //    {
        //        if (cacheManager.InvalidateCheckInterval.GetValueOrDefault().TotalSeconds < 60)
        //            cacheManager.InvalidateCheckInterval = TimeSpan.FromSeconds(60);
        //        cache.InvalidateCheckInterval = cacheManager.InvalidateCheckInterval.Value;
        //    }

        //    if (cacheManager?.ClearCache ?? false)
        //        cache.Clear();

        //    return GetCacheManagerZillow();
        //}

        #endregion

        #endregion
    */
    }
}
