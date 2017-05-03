using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using com.LoanTek.API.Pricing.Clients.Models;
using com.LoanTek.API.Pricing.Clients.Models.Common.Mortgage;
using LoanTek.Pricing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IQuote = com.LoanTek.Quoting.IQuote;

namespace com.LoanTek.API.Pricing.Clients.Models.Tests
{
    [TestClass()]
    public class CustomMortgageResponseTests
    {
 [TestMethod()]
 public void CustomMortgageResponseTest()
 {
     string fullJson = "{\"UserId\":, \"QuoteId\":, \"RSMasterId\":, \"RSLenderId\":, \"RSMasterLoanProgramId\":, \"RSRateCellId\":, \"RSRateId\":, \"RSVersionId\":, \"RSVersion\":, \"LenderName\":\"\", \"ProgramCode\":\"\", \"LoanProgramName\":\"\", \"LenderProgramName\":\"\", \"LoanProduct\":\"\", \"ProductFamily\":\"\", \"ProductClass\":\"\", \"ProductType\":\"\", \"ProductTerm\":\"\", \"AdjustmentCount\":, \"AdjustmentTotal\":, \"Adjustments\":\"\", \"QuoteLock\":, \"Price\":, \"CalcPrice\":, \"OrigFees\":1877, \"NonAprFees\":507, \"QuoteFees\":3890.2, \"GovFees\":0, \"FinalFees\":2013.2, \"QuoteRate\":3.25, \"APR\":3.347, \"PIP\":1218.58, \"MIP\":0, \"QuoteType\":\"ClosestTo01\", \"QuoteTypeSort\":, \"QuoteTypeDisplay\":\"\", \"HasAdd\":, \"StopEncountered\":, \"OfferId\":0, \"FeeSetId\":4653, \"ItemizedFees\":\"\", \"SheetActive\":true, \"LoanTerm\":360, \"FHALoan\":false, \"VALoan\":false, \"Quote\":{ \"UserId\":,    \"RsMasterLoanProgramId\":,    \"TermInMonths\":360,    \"LoanProduct\":\"BAY071\",    \"Fico\":720,    \"LoanToValue\":80,    \"LoanAmount\":280000,    \"PropertyType\":\"SFH\",    \"PropertyUsage\":\"PRI\",    \"LoanPurpose\":\"PUR\",    \"State\":\"ID\",    \"ZipCode\":\"83702\",    \"County\":\"Ada\",    \"QuoteChannel\":0,    \"Fees\":1877,    \"NonAprFees\":507,    \"OverrideFees\":false,    \"NewFees\":0,    \"OverrideComp\":false,    \"NewComp\":0,    \"FhaLoan\":false,    \"VaLoan\":,    \"CashOut\":,    \"QuoteTypesToReturn\":\"\",    \"CompUserId\":861,    \"ChannelProductName\":\"7yearARM\",    \"ChannelProductId\":0,    \"FeeSetId\":4653,    \"ItemizedFees\":\"\",    \"SheetActive\":true,    \"GovFees\":0,    \"HarpLoan\":false,    \"LenderPaidInsurance\":true,    \"ARMDetails\":{  \"IndexType\":\"\",\"Margin\":2.25,\"FixRate\":84,\"AdjustRate\":12,\"InitialCap\":5,\"PeriodicCap\":2,\"LifetimeCap\":5    },    \"LockPeriod\":30,    \"CombinedLoanToValue\":-1,    \"DebtToIncomeRatio\":0,    \"EscrowsWaived\":false,    \"NationalLender\":true,    \"ClientDefinedIdentifier\":\"1150\",    \"IgnorePricingRules\":false,    \"Disallow\":false,    \"FeeSet\":{  \"Created\":\"0001-01-01T00:00:00\",\"Updated\":\"0001-01-01T00:00:00\",\"FeeSetId\":4653,\"LoanAmount\":280000,\"ItemizedFees\":\"Description: Appraisal fee, HUD Line: 803, Fee Type: $$, Amount: $475.00, Include In APR: False\u003cbr /\u003eDescription: Credit report fee, HUD Line: 804, Fee Type: $$, Amount: $32.00, Include In APR: False\u003cbr /\u003eDescription: Processing fee, HUD Line: 811, Fee Type: $$, Amount: $495.00, Include In APR: True\u003cbr /\u003eDescription: Underwriting fee, HUD Line: 812, Fee Type: $$, Amount: $875.00, Include In APR: True\u003cbr /\u003e\",\"Fees\":[     { \"HudLine\":803, \"Description\":\"Appraisal fee\", \"IncludeInAPR\":false, \"FeeAmount\":475, \"FeeType\":1   },   { \"HudLine\":804, \"Description\":\"Credit report fee\", \"IncludeInAPR\":false, \"FeeAmount\":32, \"FeeType\":1   },   { \"HudLine\":811, \"Description\":\"Processing fee\", \"IncludeInAPR\":true, \"FeeAmount\":495, \"FeeType\":1   },   { \"HudLine\":812, \"Description\":\"Underwriting fee\", \"IncludeInAPR\":true, \"FeeAmount\":875, \"FeeType\":1   }],\"NonAPRFees\":507,\"DollarFees\":1370,\"PercentFees\":0,\"TotalFees\":1877    },    \"BranchId\":0,    \"Message\":\"Looking for more conversion on your mortgage leads? Try LoanTek\u0027s interactive website tools. Create exclusive, interactive, consumer engagements online - works great with your real estate referral partners too! Visit www.loantek.com - or Call (888) 562-6835 and find out how LoanTek lenders and brokers convert up to 4X more on their mortgage leads as compared to other competing lead management systems.\u003cbr /\u003e\\n\u003cbr /\u003e\\nNOTE: THIS DISPLAY IS FOR INFORMATIONAL PURPOSES ONLY - LOANTEK, INC. IS NOT A LENDER AND DOES NOT SOLICIT LOANS FROM CONSUMERS.\",    \"InterestOnly\":false,    \"InterestOnlyForMonths\":0,    \"NoteDueInMonths\":360,    \"PrepaymentPenalty\":false,    \"RateFixedForMonths\":84,    \"QuoteId\":104567,    \"DisallowRule\":\"\",    \"ClientId\":399,    \"CompUserBranchId\":0,    \"LeadGenerationMethodTypeId\":1,    \"IsLoanPricerRequest\":true }, \"ChannelProductName\":\"7yearARM\", \"ChannelProductId\":0, \"Guid\":\"73d5b56e-6d5d-4517-b7f1-e40cf377dcf8\", \"UFMIPPercent\":0, \"NationalLender\":true, \"ClientDefinedIdentifier\":\"1150\", \"IgnorePricingRules\":false, \"Disallow\":false, \"MIPInfo\":{ \"AnnualRate\":0,    \"AnnualPremium\":0,    \"MonthlyRate\":0,    \"MonthlyPremium\":0,    \"UpfrontRate\":0,    \"UpfrontPremium\":0 }, \"DisallowRule\":\"\", \"ClientId\":399, \"RSVersionDate\":\"2015-12-01T09:55:20.217\", \"ActualLockPeriod\":30, \"HasMaxCredit\":false, \"HiddenAdjustments\":\"\", \"RateSheetPrice\":-1.031, \"LoanRequest\":{ \"LoanRequestCreatedId\":0,    \"VALoan\":false,    \"LoanOwnedByFannie\":true,    \"LoanOwnedByFreddie\":true,    \"LoanOwnedByFHA\":true,    \"IRRRL\":false,    \"CashOut\":false,    \"HasProofOfIncome\":true,    \"DeclaredBankruptcyLast7Years\":false,    \"ForeclosedLast7Years\":false,    \"FirsttimeBuyer\":false,    \"NewConstruction\":false,    \"VADisabled\":false,    \"VAFirstTime\":false,    \"VAType\":0,    \"IncludeAllPricing\":false,    \"OverrideComp\":false,    \"NewComp\":0,    \"OverrideFees\":false,    \"NewFees\":0,    \"BorrowerPaid\":false,    \"ProductFamily\":\"\",    \"ProductClass\":\"\",    \"ProductType\":\"\",    \"ProductTerm\":\"\",    \"IncludeDisabledSheets\":,    \"EligibleForUSDALoan\":,    \"IgnoreZillowAccountBalance\":,    \"LockPeriod\":,    \"SecondMortgageAmount\":0,    \"CombinedLoanToValue\":-1,    \"DebtToIncomeRatio\":0,    \"LenderPaidInsurance\":false,    \"EscrowsWaived\":false,    \"ClientDefinedIdentifier\":\"1150\",    \"IgnorePricingRules\":false,    \"LeadGenerationMethodType\":1,    \"IsLoanPricerRequest\":true,    \"ChannelProductId\":0,    \"PropertyValue\":336000,    \"SecondMortgage\":false,    \"ShowBestExecution\":true,    \"FHALoan\":false,    \"LoanLimit\":417000,    \"IsJumboRequest\":false,    \"IsHighBalanceRequest\":false,    \"StateAbbreviation\":\"ID\",    \"StateFullName\":\"Idaho\",    \"CountyInfo\":{  \"ZipCode\":\"\",\"AreaCode\":\"\",\"City\":\"\",\"CityAlias\":\"\",\"StateName\":\"\",\"StateAbbv\":\"\",\"CountyId\":,\"CountyName\":\"\",\"MaxOneFamily\":,\"MaxTwoFamily\":,\"MaxThreeFamily\":,\"MaxFourFamily\":,\"CountySetId\":1000296,\"FHA1\":271050,\"FHA2\":347000,\"FHA3\":419425,\"FHA4\":521250,\"CountyFIP\":\"1\",\"VA1\":417000    },    \"LoanPurpose\":1,    \"ZipCode\":\"83702\",    \"LoanAmount\":280000,    \"LoanToValue\":80,    \"CreditScore\":720,    \"PropertyUsage\":1,    \"PropertyType\":1,    \"LoanProgramsOfInterest\":[  99    ],    \"QuoteTypesToReturn\":[  -1,0,1,2    ],    \"QuotingChannel\":0 }}";

     string customLoanResponseJson = "{ \"FHALoan\" : \"\", \"APR\" : \"\", \"RSVersionDate\" : \"\", \"ARMDetails\" : { \"AdjustRate\" : \"\" }, \"ChannelProductName\" : \"\",\"Quote\" : { \"QuoteTypesToReturn\" : \"\", \"UserId\" : \"\" } }";
     CustomMortgageResponse response = new CustomMortgageResponse();
     List<IQuote> quotes = new List<IQuote>();
     //for (int i = 0; i < 1; i++)
     //{
     //    quotes.Add(new Quote());       
     //}
     var startTime = DateTime.Now;
     response.ConvertCustomJsonToLoanResponse(customLoanResponseJson, quotes);
     Debug.WriteLine("quote count:" + response.Quotes.Count + " in mill: " + (DateTime.Now - startTime).TotalMilliseconds);
     var json = JsonConvert.SerializeObject(response.Quotes);
     Debug.WriteLine("CustomMortgageResponse json:\n" + json);
 }

 [TestMethod()]
 public void ConvertCustomJsonToLoanResponseTest()
 {
     string json = "{ \"FHALoan\" : \"\", \"APR\" : \"\", \"RSVersionDate\" : \"\", \"ARMDetails\" : { \"AdjustRate\" : \"\" }, \"ChannelProductName\" : \"\",\"Quote\" : { \"QuoteTypesToReturn\" : \"\", \"UserId\" : \"\" } }";
     var obj = JObject.Parse(json); //new CustomMortgageRequest().ConvertCustomJsonToLoanResponse(json);
     Rate rate = new Rate();
     rate.APR = (decimal)3.75;
     rate.QuoteRate = (decimal)3.50;
     rate.QuoteTypeSort = 1;
     rate.RSVersionDate = DateTime.Now;
     rate.ARMDetails = new ARMDetails() { AdjustRate = 1, FixRate = 2 };
     rate.FHALoan = false;
     rate.Quote = new Quote();
     rate.Quote.QuoteTypesToReturn = "ClosestToZeroNoFee|ClosestToZeroWithFee|ClosestTo01|ClosestTo02";
     rate.ChannelProductName = "20yearFixed";

     var sourceType = rate.GetType();

     foreach (var item in obj)
     {
  Debug.WriteLine("key:" + item.Key);
  //obj[item.Key] = "RATE VALUES GOES HERE";
  var sourceProperty = sourceType.GetProperty(item.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
  if (sourceProperty == null)
      continue;
  object valueToSet = sourceProperty.GetGetMethod().Invoke(rate, null);
  if (valueToSet == null)
      continue;
  if (item.Value.HasValues)
  {
      JObject token = new JObject();
      foreach (var item2 in (JObject)item.Value)
      {
   Debug.WriteLine(" nested key: " + item2.Key);
   var sourceProperty2 = valueToSet.GetType().GetProperty(item2.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
   if (sourceProperty2 == null)
continue;
   object valueToSet2 = sourceProperty2.GetGetMethod().Invoke(valueToSet, null);
   if (valueToSet2 == null)
continue;
   token[item2.Key] = JToken.FromObject(valueToSet2);
      }
      valueToSet = token;
  }
  obj[item.Key] = JToken.FromObject(valueToSet);
     }

     //json = JsonConvert.SerializeObject(obj);
     //Debug.WriteLine("response json obj:\n" + json);

     dynamic LoanResponse = Json.Decode(json);
     var apr = LoanResponse.APR;
     Assert.AreEqual(rate.APR, apr);
 }
    }
}
