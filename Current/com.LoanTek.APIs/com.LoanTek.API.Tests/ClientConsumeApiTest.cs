using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using com.LoanTek.API.Pricing.Partners.Models;
using com.LoanTek.API.Pricing.Partners.Models.Common;
using com.LoanTek.Quoting.Mortgage.PreQualUsers;
using com.LoanTek.Types;
using LoanTek.Pricing;
using LoanTek.Pricing.BusinessObjects;
using LoanTek.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using FullMortgageRequest = com.LoanTek.API.Pricing.Partners.Models.Mortgage.FullMortgageRequest;

namespace com.LoanTek.API.Tests
{
    [TestClass]
    public class ClientConsumeApiTest
    {
        private static JsonMediaTypeFormatter jsonFormat;
        private int[] userIds = { 105, 1925, 800, 1743 };
        private static ConcurrentBag<FullMortgageResponse> responses = new ConcurrentBag<FullMortgageResponse>();

        private static readonly HttpClient client = new HttpClient();
        static async Task InitClient()
        {
            Counties.Instance.InitAllCounties();

            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.MaxServicePointIdleTime = 500;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.CheckCertificateRevocationList = false;

            // New code:
            client.BaseAddress = new Uri("http://52.90.207.172:80");
            //client.BaseAddress = new Uri("http://localhost:8887");
            //client.BaseAddress = new Uri("http://partners-pricing-api-2.loantek.com");
            //client.BaseAddress = new Uri("http://10.83.95.25");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        static async Task<List<QuotingUser>> GetPeQualUsers(string path)
        {
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<List<QuotingUser>>();
            }
            return null;
        }

        static async Task<FullMortgageResponse> PostRequest(string endpoint, FullMortgageRequest request)
        {
            Debug.WriteLine(DateTime.Now.ToString("T") + " Posting for " + request.UserId + " to: " + endpoint);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            request.Form.Amount++;
            HttpResponseMessage response = await client.PostAsync<FullMortgageRequest>(endpoint, request, jsonFormat);
            //HttpResponseMessage response = await client.PostAsJsonAsync(endpoint, request);
            FullMortgageResponse fullMortgageResponse = null;
            if (response.IsSuccessStatusCode)
                fullMortgageResponse = await response.Content.ReadAsAsync<FullMortgageResponse>();
            else
                Debug.WriteLine("failed: " + response.StatusCode +" "+ response.ReasonPhrase +" "+ response.Content?.ReadAsStringAsync()?.Result);
            sw.Stop();
            responses.Add(fullMortgageResponse);
            //Debug.WriteLine("done for " + request.UserId + " in " + sw.ElapsedMilliseconds);
            return fullMortgageResponse;
        }

        [TestInitialize]
        public void Init()
        {
            jsonFormat = new JsonMediaTypeFormatter();
            jsonFormat.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
            jsonFormat.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            jsonFormat.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            InitClient().Wait();
        }

        [TestCleanup]
        public void CleanUp()
        {
            client.Dispose();
        }

        [TestMethod]
        public void ConsumeBankrateOwinServiceTest()
        {
            FullMortgageRequest request = DummyData.CreateDummyRequest(QuotingChannelType.Bankrate);
            request.UserId = 861;
            
            InitClient().Wait();

            Task<FullMortgageResponse> task = PostRequest("Bankrate/VWRsL3FrVllETEZFbnlocjZFbEJUcHRvOU1NRG11eGx3N0NZeW84YjRZWE1vc3VOY0cwdkYxMXZKWkRZU0tHc2c1VUd2MG9nMmI0QQ2/FullMortgageRequest", request);
            task.Wait();
            FullMortgageResponse response = task.Result;
            Debug.WriteLine("response:" + response.Status+" in "+ response.ExecutionTimeInMillisec);
        }

        [TestMethod]
        public void GetPeQualUsersTest()
        {
            Task<List<QuotingUser>> task = GetPeQualUsers("Bankrate/VWRsL3FrVllETEZFbnlocjZFbEJUcHRvOU1NRG11eGx3N0NZeW84YjRZWE1vc3VOY0cwdkYxMXZKWkRZU0tHc2c1VUd2MG9nMmI0QQ2/PreQualList");
            task.Wait();
            List<QuotingUser> response = task.Result;
            Debug.WriteLine("response -user count:" + response.Count + " Bankrate active count:" + response.Count(x => x.ActivePartnerPreference.QuotingChannelType == QuotingChannelType.Bankrate && x.ActivePartnerPreference.Active.GetValueOrDefault()));
            //return response;
        }

        [TestMethod]
        public void ConsumeFullBankrateRequestTest()
        {
            var startTime = DateTime.Now;
            //new PricingEngineList().InitPricingEngines(userIds.ToList());
            //Debug.WriteLine("time to init all user engines:"+ (DateTime.Now - startTime).TotalSeconds);

            consumeFullBankrateRequestTest().Wait();
        }

        private Random r = new Random();
        private async Task consumeFullBankrateRequestTest()
        {
            if (client == null)
                this.Init();

            //FullMortgageRequest request = DummyData.CreateDummyRequest(QuotingChannelType.Bankrate);
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            var path = dir + @"\..\..\FullMortgageRequest1.json";
            string json = File.ReadAllText(path);
            FullMortgageRequest request = JsonConvert.DeserializeObject<FullMortgageRequest>(json);
            request.UserId = 0;
            request.Form.Amount += DateTime.Now.Second;          

            Stopwatch sw = new Stopwatch();
            sw.Start();

            Task<List<QuotingUser>> task1 = GetPeQualUsers("Bankrate/VWRsL3FrVllETEZFbnlocjZFbEJUcHRvOU1NRG11eGx3N0NZeW84YjRZWE1vc3VOY0cwdkYxMXZKWkRZU0tHc2c1VUd2MG9nMmI0QQ2/PreQualList");
            task1.Wait();
            IEnumerable<QuotingUser> quotingUsers = task1.Result?.Where(x => x.ActivePartnerPreference?.QuotingChannelType == QuotingChannelType.Bankrate && (bool) x.ActivePartnerPreference?.Active.GetValueOrDefault());
            sw.Stop();
            Debug.WriteLine(quotingUsers.Count() +" quotingUsers in " + sw.ElapsedMilliseconds);

            var taskList = new List<Task<FullMortgageResponse>>();

            int counter = 0;

            var startTime = DateTime.Now;
            sw.Restart();

            //Parallel.ForEach(quotingUsers, (quotingUser) =>
            foreach (var quotingUser in quotingUsers)
            {
                quotingUser.UserId = userIds[r.Next(0, userIds.Length - 1)];
                //if (++counter > 55)
                //    break;

                var req = ClassMappingUtilities.CloneJson(request);
                req.UserId = quotingUser.UserId;
                req.Form.Amount += counter++;
                req.Form.LoanToValue += r.Next(-10, 20);
                //req.UserId = quotingUser.UserId;
                // by virtue of not awaiting each call, you've already acheived parallelism
                taskList.Add(PostRequest("Bankrate/VWRsL3FrVllETEZFbnlocjZFbEJUcHRvOU1NRG11eGx3N0NZeW84YjRZWE1vc3VOY0cwdkYxMXZKWkRZU0tHc2c1VUd2MG9nMmI0QQ2/FullMortgageRequest/?UseOnlyThisUserId=" + quotingUser.UserId, req));
                //taskList.Add(PostRequest("VTest/Bankrate/VWRsL3FrVllETEZFbnlocjZFbEJUcHRvOU1NRG11eGx3N0NZeW84YjRZWE1vc3VOY0cwdkYxMXZKWkRZU0tHc2c1VUd2MG9nMmI0QQ2/FullMortgageRequest/?UseOnlyThisUserId=" + quotingUser.UserId, request));
                //taskList.Add(PostRequest("Bankrate/VWRsL3FrVllETEZFbnlocjZFbEJUcHRvOU1NRG11eGx3N0NZeW84YjRZWE1vc3VOY0cwdkYxMXZKWkRZU0tHc2c1VUd2MG9nMmI0QQ2/FullMortgageRequest", req));
                //Thread.Sleep(200);
            }//);
            try
            {
                Debug.WriteLine("time to post in milli: " + sw.ElapsedMilliseconds);

                // asynchronously wait until all tasks are complete
                await Task.WhenAll(taskList.ToArray());

                //foreach (var task in taskList)
                //{
                //    Debug.WriteLine(" " + task.Status + "..."+ task.Result);
                //}
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR:" + ex.Message);
            }
            sw.Stop();

            printOut();

        }

        private void printOut()
        {
            var procList = responses.Where(x => x != null).OrderByDescending(x => x.ExecutionTimeInMillisec).ToList();
            Debug.WriteLine(responses.Count + " NOT NULL = " + procList.Count);
            for (int i = 0; i < procList.Count; i++)
            {
                var x = procList[i];
                Debug.WriteLine("\n" + i + " " + x.Status + " -TotalTime:" + x.ExecutionTimeInMillisec +" -Quote Count:"+ x.Submissions?.FirstOrDefault()?.Quotes?.Count);
            }
        }
    }
}
