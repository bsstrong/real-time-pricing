using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using com.LoanTek.API.Demo.Models.Bankrate;
using com.LoanTek.API.Demo.Models.Bankrate.Mortgage;
using com.LoanTek.API.Demo.Models.Sdk;
using com.LoanTek.API.Pricing.Partners.Controllers;
using com.LoanTek.API.Pricing.Partners.Models;
using com.LoanTek.API.Pricing.Partners.Models.Common;
using com.LoanTek.Quoting;
using com.LoanTek.Quoting.Common;
using com.LoanTek.Types;
using LoanTek.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Converter = com.LoanTek.Quoting.Bankrate.Converter;
using MortgageLoanRequest = com.LoanTek.API.Requests.MortgageLoanRequest;
using Bankrate = com.LoanTek.Quoting.Bankrate;

namespace com.LoanTek.API.Tests
{
    [TestClass]
    public class FullMortgageRequestTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            MortgageLoanRequest mortgageLoanRequest = new MortgageLoanRequest()
            {
                ZipCode = "90210",
                LoanAmount = 500000,
                CreditScore = 720,
                LoanToValue = 80,
                LockPeriod = LockPeriodType.D30,
                LoanPurpose = LoanPurposeType.Purchase,
                PropertyType = PropertyTypeType.SingleFamily,
                PropertyUsage = PropertyUseType.PrimaryResidence,
                QuoteTypesToReturn = new List<QuoteTypeType> { QuoteTypeType.ClosestToZeroNoFee },
                ProductTerm = new List<ProductTermType> { ProductTermType.F30, ProductTermType.A5_1 },
            };

            FullMortgageRequest fullRequest = new FullMortgageRequest
            {
                LoanRequest = mortgageLoanRequest,
                ClientDefinedIdentifier = "LTP" + StringUtilities.UniqueId(),
            };

            var controller = new BankrateController();
            //controller.FullMortgageRequest(
        }


        [TestMethod()]
        public void QuoteMortgageRequestBankrateTest()
        {
            this.quoteMortgageRequestBankrateTest().Wait();
        }
        public async Task quoteMortgageRequestBankrateTest()
        {
            //QuoteRequest quoter = new QuoteRequest();
            //await quoter.GetPreQualUsers();
            //Debug.WriteLine("QuotingUsers #" + QuoteMortgageRequest.QuotingUsers.Count);
            //try
            //{
            //    Pricing.Partners.Models.Mortgage.FullMortgageRequest request = DummyData.CreateDummyRequest(QuotingChannelType.Bankrate);
            //    request.Form.Amount = 300000;
            //    //if(request.Form.ProductFamilyTypes.Count > 2)
            //    //    request.Form.ProductFamilyTypes = new List<ProductFamilyType>(

            //    await quoter.Quote(request.Form, "BR-TEST"+ StringUtilities.CreateRandomString(6));
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine(ex.Message);
            //}

            //foreach (var result in quoter.Results)
            //{
            //    Debug.WriteLine("Quotes from #" + result.Content?.Submissions?.FirstOrDefault()?.Quotes?.Count);

            //}
              
            //Thread.Sleep(10000);

        }

        /// <summary>
        /// This class is used by the Json deserializer to convert the IQuoteSubmission inteface to a concrete type.
        /// </summary>
        public class BankrateConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return (objectType == typeof(IQuoteSubmission<Quote>));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                return jo.ToObject<Converter.LoanQuoteSubmission>(serializer);
            }

            public override bool CanWrite => false;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
