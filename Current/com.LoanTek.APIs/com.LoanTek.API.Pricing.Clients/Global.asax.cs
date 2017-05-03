using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using com.LoanTek.API.Pricing.Clients.Controllers;
using com.LoanTek.API.Pricing.Clients.Models;
using com.LoanTek.Biz.Api.Objects;
using com.LoanTek.Biz.Pricing.Objects;
using com.LoanTek.IData;
using com.LoanTek.Types;
using LoanTek.Utilities;
using CustomExceptionFilterAttribute = com.LoanTek.API.Pricing.Clients.Filters.CustomExceptionFilterAttribute;
using SearchAndCompare = LoanTek.Utilities.SearchAndCompare;

namespace com.LoanTek.API.Pricing.Clients
{
    public class Global : Common.Global
    {
        public static IApiExplorer ApiExplorer;
        public static List<Partner> Partners;
        public static List<ApiWebService> ClientWebServices;

        public static void InitData()
        {
            Debug.WriteLine("Pricing.Clients.Global.InitData() called...");

            ApiCategoryType = Types.Api.ApiCategoryTypes.Pricing;
            ApiForWhoType = Types.Api.ApiForWhoTypes.Clients;

            #region ApiObject

            ApiObject = new PricingClientsApi()
            {
                Versions = new List<Version>()
                {
                    new Version()
                    {
                        MajorVersionId = 1,
                        MinorVersionId = 0,
                        VersionStatus = Types.Api.VersionStatusType.Beta,
                        Created = Convert.ToDateTime("03/01/2016"),
                        LastUpdated = Convert.ToDateTime("03/08/2016")
                    }
                }
            };

            #endregion

            //Partners.Global.InitData();
            initCommonPricingData();
        }

        private static bool isRunning;
        private static void initCommonPricingData()
        {
            if (isRunning)
                return;
            isRunning = true;

            Partners = new Biz.Pricing.Objects.Partners(DataContextType.Database).Get();

            SearchAndCompare.ColumnSearchItem item1 = new SearchAndCompare.ColumnSearchItem() { Column = "CategoryType", CompareType = Types.SearchAndCompare.CompareType.EqualTo, SearchTerm = ApiCategoryType.ToString()};
            SearchAndCompare.ColumnSearchItem item2 = new SearchAndCompare.ColumnSearchItem() { Column = "WhoForType", CompareType = Types.SearchAndCompare.CompareType.EqualTo, SearchTerm = ApiForWhoType.ToString() };
            Filter filter = new Filter();
            filter.PropertySearchItems = new List<SearchAndCompare.ColumnSearchItem>() { item1, item2 };
            ClientWebServices = new ApiWebServices(DataContextType.Database, DataConnections.DataContextLoanTekRead).Get(filter);

            isRunning = false;
        }

        protected void Application_Start()
        {
            var startTime = DateTime.Now;

            new DataConnections(); //init a new DataConnections to load any connection strings from the 'Settings' file

            InitData();

            GlobalConfiguration.Configure(WebApiConfig.Register);
            GlobalConfiguration.Configuration.Filters.Add(new CustomExceptionFilterAttribute());

            ApiExplorer = GlobalConfiguration.Configuration.Services.GetApiExplorer(); 

            var clientId = 13;
            Debug.WriteLine("Test MortgageLoanController Auth key for clientId:"+ clientId + " = " + AuthToken.EncryptAuthKey(clientId, MortgageLoanController.ApiWebService.WebServiceName));
            Debug.WriteLine("Test AutoLoanController Auth key for clientId:" + clientId + " = " + AuthToken.EncryptAuthKey(clientId, AutoLoanController.ApiWebService.WebServiceName));
            Debug.WriteLine("Test DepositController Auth key for clientId:" + clientId + " = " + AuthToken.EncryptAuthKey(clientId, DepositYieldController.ApiWebService.WebServiceName));
            clientId = 399;
            Debug.WriteLine("Test MortgageLoanController Auth key for clientId:" + clientId + " = " + AuthToken.EncryptAuthKey(clientId, MortgageLoanController.ApiWebService.WebServiceName));
            Debug.WriteLine("Test AutoLoanController Auth key for clientId:" + clientId + " = " + AuthToken.EncryptAuthKey(clientId, AutoLoanController.ApiWebService.WebServiceName));
            Debug.WriteLine("Test DepositController Auth key for clientId:" + clientId + " = " + AuthToken.EncryptAuthKey(clientId, DepositYieldController.ApiWebService.WebServiceName));

            string key = ConfigurationManager.AppSettings.AllKeys.FirstOrDefault(x => x.ToLower().Equals("debugmodetype"));
            DebugModeType = EnumLib.TryParse<Processing.DebugModeType>((key != null) ? ConfigurationManager.AppSettings[key] : Processing.DebugModeType.None.ToString());

            key = ConfigurationManager.AppSettings.AllKeys.FirstOrDefault(x => x.ToLower().Equals("useonlythisuserid"));
            UseOnlyThisUserId = NullSafe.NullSafeInteger(ConfigurationManager.AppSettings[key], 0);

            key = ConfigurationManager.AppSettings.AllKeys.FirstOrDefault(x => x.ToLower().Equals("Types.Api.ServerStatusType"));
            ServerStatusType = EnumLib.TryParse<Types.Api.ApiStatusType>((key != null) ? ConfigurationManager.AppSettings[key] : ((Debugger.IsAttached) ? Types.Api.ApiStatusType.Testing : Types.Api.ApiStatusType.Live).ToString());

            Debug.WriteLine("Total init time:" + (DateTime.Now - startTime).TotalSeconds);
        }
    }

}
