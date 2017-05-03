using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using com.LoanTek.API.Leads.Clients.Controllers;
using com.LoanTek.API.Leads.Clients.Filters;
using com.LoanTek.API.Leads.Clients.Models;
using com.LoanTek.Biz.Api.Objects;
using com.LoanTek.CRM.Files.Assets;
using com.LoanTek.CRM.Files.ContactMethods;
using com.LoanTek.CRM.Files.Debts;
using com.LoanTek.CRM.Files.Persons;
using com.LoanTek.IData;
using LoanTek.Pricing.BusinessObjects;
using LoanTek.Utilities;


namespace com.LoanTek.API.Leads.Clients
{
    public class Global : com.LoanTek.API.Common.Global
    {
        static Global()
        {
            //Needed for conversion of abstract lists
            JsonSettings.Converters.Add(new APerson.ConcreteConverter());
            JsonSettings.Converters.Add(new AContactMethod.ConcreteConverter());
            JsonSettings.Converters.Add(new AAsset.ConcreteConverter());
            JsonSettings.Converters.Add(new ADebt.ConcreteConverter());
        }

        public static IApiExplorer ApiExplorer;
        //public static List<Partner> Partners;
        public static List<ApiWebService> LeadWebServices;

        private static bool doOnce;
        private void initCommonData()
        {
            if (doOnce)
                return;
            doOnce = true;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            Counties.GetAllCounties();
            Debug.WriteLine(" -GetAllCounties time in millisec:" + sw.ElapsedMilliseconds);
            sw.Restart();
            Master.Lists.Clients.GetAllClients();
            Master.Lists.Users.GetAllUsers();
            Debug.WriteLine(" -GetAllClients & GetAllUsers time in millisec:" + sw.ElapsedMilliseconds);
        }

        protected void Application_Start()
        {
            new DataConnections();

            var formatters = GlobalConfiguration.Configuration.Formatters;
            var jsonFormatter = formatters.JsonFormatter;
            jsonFormatter.SerializerSettings = JsonSettings;

            LeadWebServices = new ApiWebServices(DataContextType.Database, DataConnections.DataContextLoanTekRead).Get(new Filter
            {
                PropertySearchItems = new List<SearchAndCompare.ColumnSearchItem>
                {
                    new SearchAndCompare.ColumnSearchItem
                    {
                        Column = "CategoryType",
                        CompareType = Types.SearchAndCompare.CompareType.EqualTo,
                        SearchTerm = Types.Api.ApiCategoryTypes.CRM.ToString()
                    }
                }
            });


            GlobalConfiguration.Configure(WebApiConfig.Register);
            GlobalConfiguration.Configuration.Filters.Add(new CustomExceptionFilterAttribute());

            ApiObject = new LeadsClientsApi()
            {
                Versions = new List<Version>()
                {
                    new Version()
                    {
                        MajorVersionId = 1,
                        MinorVersionId = 1,
                        VersionStatus = Types.Api.VersionStatusType.Beta,
                        Created = Convert.ToDateTime("04/01/2016"),
                        LastUpdated = Convert.ToDateTime("07/08/2016")
                    }
                }
            };

            var clientId = 399;
            Debug.WriteLine("ClientId:"+ clientId + " LeadsController Auth key:" + AuthToken.EncryptAuthKey(clientId, NewLeadsController.ApiObject.ApiName));
            Debug.WriteLine("ClientId:" + clientId + " WidgetController Auth key:" + AuthToken.EncryptAuthKey(clientId, WidgetController.WebServiceName));

            string key = ConfigurationManager.AppSettings.AllKeys.FirstOrDefault(x => x.ToLower().Equals("useonlythisuserid"));
            Global.UseOnlyThisUserId = NullSafe.NullSafeInteger(ConfigurationManager.AppSettings[key], 0);

            key = ConfigurationManager.AppSettings.AllKeys.FirstOrDefault(x => x.ToLower().Equals("Types.Api.ServerStatusType"));
            Global.ServerStatusType = EnumLib.TryParse<Types.Api.ApiStatusType>((key != null) ? ConfigurationManager.AppSettings[key] : ((Debugger.IsAttached) ? Types.Api.ApiStatusType.Testing : Types.Api.ApiStatusType.Live).ToString());

            if (!Debugger.IsAttached)
                this.initCommonData();
        }
    }

}
