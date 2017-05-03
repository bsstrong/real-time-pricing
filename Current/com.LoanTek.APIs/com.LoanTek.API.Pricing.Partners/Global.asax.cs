using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using com.LoanTek.API.Pricing.Partners.Controllers;
using com.LoanTek.API.Pricing.Partners.Models;
using com.LoanTek.Biz.Api.Objects;
using com.LoanTek.IData;
using com.LoanTek.Types;
using LoanTek.Utilities;
using BusinessObjects = LoanTek.Pricing.BusinessObjects;
using Partner = com.LoanTek.Biz.Pricing.Objects.Partner;

namespace com.LoanTek.API.Pricing.Partners
{
    public class Global : Common.Global
    {
        //public static StateRule FilterRequestByStateRule { get; set; }
        public static IApiExplorer ApiExplorer;
        public static List<Partner> Partners;
        public static List<ApiWebService> PartnerWebServices;

        public static void InitData()
        {
            #region ApiObject

            ApiObject = new PricingPartnersApi()
            {
                Versions = new List<Version>()
                {
                    new Version()
                    {
                        MajorVersionId = 1,
                        MinorVersionId = 3,
                        VersionStatus = Types.Api.VersionStatusType.Current,
                        Created = Convert.ToDateTime("03/01/2016"),
                        LastUpdated = DateTime.Today,
                        ReleaseNotes = "PreQual Changes and moving to 'Rules' based system."
                    }
                }
            };

            #endregion

            initCommonPricingData();
        }

        private static bool isRunning;
        private static void initCommonPricingData()
        {   
            if (isRunning)  
                return;
            isRunning = true;

            Debug.Write("initCommonPricingData");

            Stopwatch sw = new Stopwatch();
            sw.Start();
            Stopwatch sw2 = new Stopwatch();
            sw2.Start();

            BusinessObjects.Counties.GetAllCounties();

            Debug.Write(" -GetAllCounties:" + sw2.ElapsedMilliseconds +"ms.");
            sw2.Restart();

            Partners = new Biz.Pricing.Objects.Partners(DataContextType.Database, DataConnections.DataContextLoanTekRead).Get();
            PartnerWebServices = new List<ApiWebService>();
            Partners.ForEach(x => PartnerWebServices.AddRange(new ApiWebServices(DataContextType.Database, DataConnections.DataContextLoanTekRead).GetByPartnerId(x.Id)));

            Debug.Write(" -Partners & PartnerWebServices:" + sw2.ElapsedMilliseconds + "ms.");
            sw2.Restart();

            //init Controller
            Task.Run(() => CommonController.RoutePrefix).ConfigureAwait(false);
            Task.Run(() => BankrateController.RoutePrefix).ConfigureAwait(false);
            Task.Run(() => ZillowController.RoutePrefix).ConfigureAwait(false);

            Debug.Write(" -Task(s) Run:" + sw2.ElapsedMilliseconds + "ms.");
            sw2.Stop();

            #region preload pricing engines
            /*
            if (false)//!Debugger.IsAttached)
            {
                //Get Default Pricing for a few users...
                List<int> userIds;
                using (QuotingDataContext dc = new QuotingDataContext())
                {
                    userIds = dc.MortgageQuote_Submissions.OrderByDescending(x => x.Id).Take(50).Select(x => x.UserId).Distinct().Take(10).ToList();
                }

                var tasks = new List<Task>();
                var saveTask = new Task(() =>
                {
                    int counter = 0;
                    Parallel.ForEach(userIds, userId =>
                    //foreach (var userId in userIds)
                    {
                        try
                        {
                            Thread.Sleep(++counter*500);
                            var time = DateTime.Now;
                            PricingEngineListNoLock2.GetPricingEngine(userId);
                            Debug.WriteLine(" -GetPricingEngine for " + userId + " -time to init: " + (DateTime.Now - time).TotalMilliseconds + " -time from start:" + (DateTime.Now - startTime).TotalMilliseconds);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Error:" + ex.Message);
                            //errorLogger.Log(SimpleLogger.LogLevelType.ERROR, string.Format("An error occured Pricing and Sending Quotes to Zillow for {0}, {1}", userId, ex.Message),
                        }
                    });
                });
                saveTask.Start();
                tasks.Add(saveTask);
                Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(60));
            }
            */
            #endregion

            isRunning = false;
            Debug.Write(" -Total time:" + sw.ElapsedMilliseconds + "ms.");
        }

        protected void Application_Start()
        {
            var startTime = DateTime.Now;

            new DataConnections(); //init a new DataConnections to load any connection strings from the 'Settings' file

            InitData();

            GlobalConfiguration.Configure(WebApiConfig.Register);

            ApiExplorer = GlobalConfiguration.Configuration.Services.GetApiExplorer();


            string key = ConfigurationManager.AppSettings.AllKeys.FirstOrDefault(x => x.ToLower().Equals("debugmodetype"));
            DebugModeType = EnumLib.TryParse<Processing.DebugModeType>((key != null) ? ConfigurationManager.AppSettings[key] : Processing.DebugModeType.None.ToString());

            key = ConfigurationManager.AppSettings.AllKeys.FirstOrDefault(x => x.ToLower().Equals("useonlythisuserid"));
            UseOnlyThisUserId = NullSafe.NullSafeInteger(ConfigurationManager.AppSettings[key], 0);

            key = ConfigurationManager.AppSettings.AllKeys.FirstOrDefault(x => x.ToLower().Equals("Types.Api.ServerStatusType"));
            ServerStatusType = EnumLib.TryParse<Types.Api.ApiStatusType>((key != null) ? ConfigurationManager.AppSettings[key] : ((Debugger.IsAttached) ? Types.Api.ApiStatusType.Testing : Types.Api.ApiStatusType.Live).ToString());

            //Debug.WriteLine(ConfigurationManager.AppSettings["ARule"]);
            //key = ConfigurationManager.AppSettings.AllKeys.FirstOrDefault(x => x.ToLower().Equals("processonlythesestatesrule"));
            //if (key != null)
            //{
            //    FilterRequestByStateRule = JsonConvert.DeserializeObject<StateRule>(ConfigurationManager.AppSettings[key]);
            //    Debug.WriteLine("Is CA match for rule:" + FilterRequestByStateRule.IsMatch("CA"));
            //}
            System.Net.ServicePointManager.DefaultConnectionLimit = int.MaxValue;

            if (Debugger.IsAttached)
            {
                string apiEndPoint = "FullMortgageRequest/POST";

                new BankrateController();
                Debug.WriteLine("Test Zillow Auth key -" + ZillowController.Partner.ApiPartnerId + "|" + ZillowController.ApiWebService.WebServiceName + " = " + AuthToken.EncryptAuthKey(ZillowController.Partner.ApiPartnerId, ZillowController.ApiWebService.WebServiceName));
                Debug.WriteLine("Test Bankrate Auth key -" + BankrateController.Partner.ApiPartnerId + "|" + BankrateController.ApiWebService.WebServiceName + " = " + AuthToken.EncryptAuthKey(BankrateController.Partner.ApiPartnerId, BankrateController.ApiWebService.WebServiceName));

                Partner partner = Partners.FirstOrDefault(x => x.GetQuotingChannelTypes()[0] == QuotingChannelType.LoanTek);
                ApiWebService apiWebService = PartnerWebServices.FirstOrDefault(x => x.PartnerId == partner?.Id && apiEndPoint.ToLower().StartsWith(x.EndPoint.ToLower()));
                if (apiWebService != null) Debug.WriteLine("Test LoanTek Auth key -" + partner?.ApiPartnerId + "|" +  AuthToken.EncryptAuthKey(partner?.ApiPartnerId ?? 0, apiWebService.WebServiceName));

                partner = Partners.FirstOrDefault(x => x.GetQuotingChannelTypes()[0] == QuotingChannelType.QuinStreet);
                apiWebService = PartnerWebServices.FirstOrDefault(x => x.PartnerId == partner?.Id && apiEndPoint.ToLower().StartsWith(x.EndPoint.ToLower()));
                if (apiWebService != null) Debug.WriteLine("Test QuinStreet Auth key -" + partner?.ApiPartnerId + "|" + AuthToken.EncryptAuthKey(partner?.ApiPartnerId ?? 0, apiWebService.WebServiceName));

                partner = Partners.FirstOrDefault(x => x.GetQuotingChannelTypes()[0] == QuotingChannelType.Brokermatch);
                apiWebService = PartnerWebServices.FirstOrDefault(x => x.PartnerId == partner?.Id && apiEndPoint.ToLower().StartsWith(x.EndPoint.ToLower()));
                if (apiWebService != null) Debug.WriteLine("Test Brokermatch Auth key -" + partner?.ApiPartnerId + "|" + AuthToken.EncryptAuthKey(partner?.ApiPartnerId ?? 0, apiWebService.WebServiceName));

                Debug.WriteLine("Total init time:" + (DateTime.Now - startTime).TotalSeconds);
            }
        }

        
    }

}
