using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.LoanTek.API;
using com.LoanTek.API.Cache;
using com.LoanTek.Types;
using LoanTek.Pricing;
using LoanTek.Pricing.LoanRequests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace com.LoanTek.API.Tests
{
    [TestClass()]
    public class CacheTests
    {

        [TestMethod()]
        public void ACacheTest()
        {
            ICache cache1 = TestCache.Instance;
            ICache cache2 = Test2Cache.Instance;
            ICachedObject obj = new CachedObject();
            for (int i = 0; i < 5; i++)
            {
                cache1.Add(i.ToString(), obj);
                cache2.Add(i.ToString(), obj);
            }
            Debug.WriteLine("Cache1 size: " + cache1.CachedData.Count());
            Debug.WriteLine("Cache2 size: " + cache2.CachedData.Count());
            Assert.AreEqual(5, cache1.CachedData.Count());
            Assert.AreEqual(5, cache2.CachedData.Count());

            for (int i = 0; i < 10; i++)
            {
                cache2.Add(i.ToString(), obj);
            }
            Debug.WriteLine("Cache1 size: " + cache1.CachedData.Count());
            Debug.WriteLine("Cache2 size: " + cache2.CachedData.Count());
            Assert.AreEqual(5, cache1.CachedData.Count()); 
            Assert.AreEqual(10, cache2.CachedData.Count());

            for (int i = 0; i < 5; i++)
            {
                cache1.Remove(i.ToString());
            }
            Debug.WriteLine("Cache1 size: " + cache1.CachedData.Count());
            Debug.WriteLine("Cache2 size: " + cache2.CachedData.Count());
            Assert.AreEqual(0, cache1.CachedData.Count());

            cache1.Add("abc".ToString(), obj);
            cache2.Add("abc", obj); 
            Debug.WriteLine("Cache1 size: " + cache1.CachedData.Count());
            Debug.WriteLine("Cache2 size: " + cache2.CachedData.Count());
            cache1.Remove("abc");
            Assert.AreEqual(0, cache1.CachedData.Count());
            Assert.AreEqual(11, cache2.CachedData.Count());
        }

        [TestMethod()]
        public void HashCkTest()
        {
            string dummyResponseData = File.ReadAllText("C:\\Users\\Eric.Tjemsland\\Documents\\Visual Studio 2013\\Projects\\com.LoanTek.APIs\\com.LoanTek.API.Tests\\LoanPricerLoanRequest-response-json.txt");

            LoanPricerLoanRequest request = new LoanPricerLoanRequest();
            request.CreditScore = 700;
            request.LoanAmount = 350000;
            request.LoanProgramsOfInterest = new List<com.LoanTek.Types.LoanProgramType>() { Types.LoanProgramType.FiveYearARM, LoanProgramType.SevenYearARM, LoanProgramType.TenYearARM };
            request.LoanPurpose = LoanPurposeType.Purchase;
            request.LoanToValue = 80;
            request.PropertyType = PropertyTypeType.SingleFamily;
            request.PropertyUsage = PropertyUseType.PrimaryResidence;
            request.QuoteTypesToReturn = new List<com.LoanTek.Types.QuoteTypeType>() { QuoteTypeType.ClosestTo01, QuoteTypeType.ClosestToZeroNoFee, QuoteTypeType.ClosestToZeroWithFee };
            request.ZipCode = "90808";
            request.QuotingChannel = QuotingChannelType.LoanTek;
            request.VALoan = true;
            request.LockPeriod = LockPeriodType.ClosestTo30;

            //MinimumLoanRequest loanRequest = new MinimumLoanRequest(request.QuotingChannel, request.CreditScore, request.LoanAmount, request.LoanPurpose, request.LoanProgramsOfInterest, request.CreditScore, request.PropertyType, request.PropertyUsage, request.ZipCode);
            //LoanPricerLoanRequestMinimized loanRequest = new LoanPricerLoanRequestMinimized(request);

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.Formatting = Formatting.None;
            settings.DefaultValueHandling = DefaultValueHandling.Ignore;

            int count = 20;
            int found = 0;
            int notFound = 0;
            double totalCacheLookup = 0;

            Random r = new Random();
            string s = null;
            for (int i = 0; i < count; i++)
            //Parallel.For(0, count, i =>
            {
                var loanRequest = newRequest(r, request);

                DateTime start = DateTime.Now;
                s = JsonConvert.SerializeObject(loanRequest, settings);
                //s = s.Substring(0, 200);
                //Debug.WriteLine(s.Length);
                //string hash = s;//s.GetHashCode().ToString();
                CachedJson response = (CachedJson)Cache.TestCache.Instance.Exists(s);
                totalCacheLookup += (DateTime.Now - start).TotalMilliseconds;
                Cache.Test2Cache.Instance.Add(s, new CachedJson());
                Debug.WriteLine("Cache2 size: " + Cache.Test2Cache.Instance.CachedData.Count());

                if (response == null)
                {
                    CachedJson cachedObject = new CachedJson();
                    cachedObject.Key = s;
                    cachedObject.Data = dummyResponseData;
                    cachedObject.CreateDummyIds();
                    //cachedObject.CreateDummyTags();
                    Cache.TestCache.Instance.Add(s, cachedObject);
                    notFound++;
                } 
                else
                {
                    Debug.WriteLine("Cache Found 1 size:" + Cache.TestCache.Instance.CachedData.Count());
                    Debug.WriteLine("Cache2 size: " + Cache.Test2Cache.Instance.CachedData.Count());
                    if (!response.Key.Equals(s))
                        throw new Exception("Cache Found: " + s + " but request do NOT match:\ncache request:\n:" + response + "\nrequest:\n" + s);
                }

                //expire
                new Task(() =>
                {
                    if (r.Next(0, 10) < 4)
                    {
                        int id = r.Next(0, 100);
                        //Cache.TestCache.Instance.RemoveById(id);
                        //Debug.WriteLine("Cache1 size: " + Cache.TestCache.Instance.CachedData.Count());
                        //Debug.WriteLine("Cache2 size: " + Cache.Test2Cache.Instance.CachedData.Count());
                    }
                });//.Start();
                Thread.Sleep(r.Next(10, 50));
            }//);
            Cache.Test2Cache.Instance.Remove(s);
            Debug.WriteLine("Avg time for cache lookup: " + totalCacheLookup + " / " + count + "=" + (totalCacheLookup / count));
            Debug.WriteLine("Cache1 size: " + Cache.TestCache.Instance.CachedData.Count());
            Debug.WriteLine("Cache2 size: " + Cache.Test2Cache.Instance.CachedData.Count());
        }

        [TestMethod()]
        public void CacheCkTest()
        {
            string dummyResponseData = File.ReadAllText("C:\\Users\\Eric.Tjemsland\\Documents\\Visual Studio 2013\\Projects\\com.LoanTek.APIs\\com.LoanTek.API.Tests\\LoanPricerLoanRequest-response-json.txt");

            //generic LoanRequest
            LoanPricerLoanRequest request = new LoanPricerLoanRequest();
            request.CreditScore = 700;
            request.LoanAmount = 350000;
            request.LoanProgramsOfInterest = new List<com.LoanTek.Types.LoanProgramType>() {Types.LoanProgramType.FiveYearARM, LoanProgramType.SevenYearARM, LoanProgramType.TenYearARM};
            request.LoanPurpose = LoanPurposeType.Purchase;
            request.LoanToValue = 80;
            request.PropertyType = PropertyTypeType.SingleFamily;
            request.PropertyUsage = PropertyUseType.PrimaryResidence;
            request.QuoteTypesToReturn = new List<com.LoanTek.Types.QuoteTypeType>() {QuoteTypeType.ClosestTo01, QuoteTypeType.ClosestToZeroNoFee, QuoteTypeType.ClosestToZeroWithFee};
            request.ZipCode = "90808";
            request.QuotingChannel = QuotingChannelType.LoanTek;
            request.VALoan = true;
            request.LockPeriod = LockPeriodType.ClosestTo30;
            
            //MinimumLoanRequest loanRequest = new MinimumLoanRequest(request.QuotingChannel, request.CreditScore, request.LoanAmount, request.LoanPurpose, request.LoanProgramsOfInterest, request.CreditScore, request.PropertyType, request.PropertyUsage, request.ZipCode);
            //LoanPricerLoanRequestMinimized loanRequest = new LoanPricerLoanRequestMinimized(request);

            Debug.WriteLine("Cache size before loop: " + Cache.TestCache.Instance.CachedData.Count());
            Cache.TestCache.Instance.SlidingExpiration = TimeSpan.FromMinutes(10);

            Random r = new Random();

            int count = 100;
            int found = 0;
            int notFound = 0;
            double totalCacheLookup = 0;
            double totalMilliFound = 0;
            double totalMilliNotFound = 0;
            //add data
            for (int i = 0; i < count; i++)
            //Parallel.For(0, count, i =>
            {
                //Thread.Sleep(r.Next(25, 50));

                var loanRequest = newRequest(r, request);

                DateTime start = DateTime.Now;
                var s = JsonConvert.SerializeObject(loanRequest);
                var response = Cache.TestCache.Instance.Exists(s);
                totalCacheLookup += (DateTime.Now - start).TotalMilliseconds;

                if (response == null)
                {
                    List<Rate> rates = null;
                    #region TESTING
                    //for testing
                    if (true)
                    {/*
                        rates = new List<Rate>();
                        for (int ii = 0; ii < 10; ii++)
                        {
                            int x = r.Next(0, 10);
                            Rate rate = new Rate();
                            rate.APR = (decimal)(3.50 + r.NextDouble());
                            rate.QuoteRate = (decimal)(3.00 + r.NextDouble());
                            rate.QuoteTypeSort = (x < 5) ? 1 : 2;
                            rate.RSVersionDate = DateTime.Now;
                            rate.ARMDetails = new ARMDetails() { AdjustRate = 1, FixRate = 2 };
                            rate.FHALoan = (x < 2);
                            rate.VALoan = (x < 5);
                            rate.Quote = new Quote();
                            rate.Quote.QuoteTypesToReturn = "ClosestToZeroNoFee|ClosestToZeroWithFee|ClosestTo01|ClosestTo02";
                            rate.ChannelProductName = "20yearFixed";
                            rates.Add(rate);
                        }*/
                        //Thread.Sleep(r.Next(20, 40));
                    }
                    #endregion
                    else
                    {
                        PricingEngine pricingEngine = PricingEngineList.GetPricingEngine(861);
                        rates = pricingEngine.PriceLoanRequest(loanRequest, BestExecutionMethodType.ByPointGroup);
                    }
                    CachedObject cachedObject = new CachedObject();
                    cachedObject.Data = dummyResponseData;
                    cachedObject.CreateDummyIds();
                    cachedObject.CreateDummyTags();
                    Cache.TestCache.Instance.Add(s, cachedObject);
                    Debug.WriteLine("cache NOT found, new request added to cache:"+ TestCache.Instance.CachedData.Count);
                    notFound++;
                    totalMilliNotFound += (DateTime.Now - start).TotalMilliseconds;
                }
                else
                {
                    //Debug.WriteLine("cache FOUND!!!: request: \n" + s);
                    found++;
                    totalMilliFound += (DateTime.Now - start).TotalMilliseconds;
                }
                /*
                //expire
                new Task(() =>
                {
                    if (r.Next(0, 10) < 6)
                    {
                        int id = r.Next(0, 100);
                        Cache.TestCache.Instance.RemoveById(id);
                    }
                }).Start();
                */
            }//);

            long c1 = Cache.TestCache.Instance.CachedData.Count();
            Debug.WriteLine("\nTotal Found: " + found + "("+ totalMilliFound +") avg milli sec: " + totalMilliFound + " / " + found + "=" + (totalMilliFound / found));
            Debug.WriteLine("Total NOT Found: " + notFound + "(" + totalMilliNotFound + ") avg milli sec: " + totalMilliNotFound + " / " + notFound + "=" + (totalMilliNotFound / notFound));
            Debug.WriteLine("Avg time for cache lookup: " + totalCacheLookup +" / " + count +"="+ (totalCacheLookup / count));
            Debug.WriteLine("Cache size after loop: " + c1);
            Thread.Sleep(10000);
            //Cache.TestCache.Instance.CachedData.Count();
            //Thread.Sleep(5000);
           // Debug.WriteLine("Cache size after & 40 sec wait: " + Cache.TestCache.Instance.CachedData.Count());
        }

        private MinimumLoanRequest newRequest(Random random, MinimumLoanRequest request)
        {
            
            int random1 = random.Next(0, 10);
            int random2 = random.Next(0, 10);
            if (random1 < 2)
                request.LoanToValue = (random2 > 8) ? 80 : (random2 > 5) ? 75 : (random2 > 2) ? 70 : 65;
            if (random1 < 4)
                request.CreditScore = (random2 > 7) ? 800 : (random2 > 5) ? 700 : (random2 > 2) ? 600 : 500;
            if (random1 < 5)
                request.PropertyType = (random2 > 7) ? PropertyTypeType.Condo : (random2 > 4) ? PropertyTypeType.Townhouse : (random2 > 1) ? PropertyTypeType.MobileOrManufactured : PropertyTypeType.SingleFamily;
            if (random1 < 7)
                request.PropertyUsage = (random2 > 7) ? PropertyUseType.PrimaryResidence : (random2 > 4) ? PropertyUseType.SecondaryOrVacation : PropertyUseType.InvestmentOrRental;
            if (random1 < 8)
                request.LoanAmount = (random2 > 7) ? 400000 : (random2 > 5) ? 550000 : (random2 > 3) ? 200000 : 600000;
            if (random1 < 10)
                request.LoanPurpose = (random2 > 6) ? LoanPurposeType.Refinance : LoanPurposeType.Purchase;
            if (random1 < 7)
                request.QuoteTypesToReturn = (random2 > 6) ? new List<QuoteTypeType>() { QuoteTypeType.ClosestTo01 } : new List<QuoteTypeType>() { QuoteTypeType.ClosestTo03 };
            //if (random2 < 5)
                //request.VALoan = false;
            /*
            switch (random2)
            {
                case 0: request.LockPeriod = LockPeriodType.D30; break;
                case 2: request.LockPeriod = LockPeriodType.D60; break;
                case 4: request.LockPeriod = LockPeriodType.D90; break;
                case 6: request.LockPeriod = LockPeriodType.D150; break;
                case 8: request.LockPeriod = LockPeriodType.D180; break;
            }*/
            switch (random2)
            {
                case 0: request.ZipCode = "83642"; break;
                case 1: request.ZipCode = "10001"; break;
                case 2: request.ZipCode = "94501"; break;
                case 3: request.ZipCode = "94601"; break;
                case 4: request.ZipCode = "94112"; break;
                case 5: request.ZipCode = "83702"; break;
                case 6: request.ZipCode = "83642"; break;
                case 7: request.ZipCode = "10001"; break;
                case 8: request.ZipCode = "94501"; break;
            }
            return request;
        }

        //private LoanPricerLoanRequestMinimized newRequest(Random random, LoanPricerLoanRequestMinimized request)
        //{
        //    int random1 = random.Next(0, 10);
        //    int random2 = random.Next(0, 10);
        //    int random3 = random.Next(10, 100);
        //    request.LoanToValue = (random1 * 10);
        //    request.CreditScore = (random3 * 10);
        //    request.FirsttimeBuyer = (random1 < 5);  
        //    request.VALoan = (random2 < 5);
        //    request.DeclaredBankruptcyLast7Years = (random3 < 50); 
        //    if (random1 < 5)
        //        request.PropertyType = (random2 > 7) ? PropertyTypeType.Condo : (random2 > 4) ? PropertyTypeType.Townhouse : (random2 > 1) ? PropertyTypeType.MobileOrManufactured : PropertyTypeType.SingleFamily;
        //    if (random1 < 7)
        //        request.PropertyUsage = (random2 > 7) ? PropertyUseType.PrimaryResidence : (random2 > 4) ? PropertyUseType.SecondaryOrVacation : PropertyUseType.InvestmentOrRental;
        //    if (random1 < 8)
        //        request.LoanAmount = (random2 > 8) ? 400000 : (random2 > 5) ? 550000 : (random2 > 3) ? 200000 : (random2 > 1) ? 100000 : 600000;
        //    if (random1 < 10)
        //        request.LoanPurpose = (random2 > 6) ? LoanPurposeType.Refinance : LoanPurposeType.Purchase;
        //    if (random1 < 7)
        //        request.QuoteTypesToReturn = (random2 > 6) ? new List<QuoteTypeType>() { QuoteTypeType.ClosestTo01 } : new List<QuoteTypeType>() { QuoteTypeType.ClosestTo03 };    
        //    switch (random2)
        //    {
        //        case 0: request.LockPeriod = LockPeriodType.D30; break;
        //        case 2: request.LockPeriod = LockPeriodType.D60; break;
        //        case 4: request.LockPeriod = LockPeriodType.D90; break;
        //        case 6: request.LockPeriod = LockPeriodType.D150; break;
        //        case 8: request.LockPeriod = LockPeriodType.D180; break;
        //    }
        //    switch (random2)
        //    {
        //        case 0: request.ZipCode = "83642"; break;
        //        case 1: request.ZipCode = "10001"; break;
        //        case 2: request.ZipCode = "94501"; break;
        //        case 3: request.ZipCode = "94601"; break;
        //        case 4: request.ZipCode = "94112"; break;
        //    }
        //    return request;
        //}
    }
}
