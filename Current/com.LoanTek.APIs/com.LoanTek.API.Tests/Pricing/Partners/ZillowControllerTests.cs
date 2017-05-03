using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using com.LoanTek.API.Pricing.Partners.Controllers;
using LoanTek.Pricing.Zillow;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace com.LoanTek.API.Tests.Pricing.Partners
{
    [TestClass()]
    public class ZillowControllerTests
    {
        private readonly Random r = new Random();

        public ZillowControllerTests()
        {
            init();
        }

        private void init()
        {
            var startTime = DateTime.Now;
            global::LoanTek.Pricing.BusinessObjects.Counties.GetAllCounties();
            Debug.WriteLine(" -GetAllCounties time in millisec:" + (DateTime.Now - startTime).TotalMilliseconds);
        }

        [TestMethod()]
        public void MortgageRequestLoadTestTest()
        {
            string authToken = "cHJ0MlhlTVQ5bm1pK1Z2S0FLVEk1N0RhZ0I1eDVhYlVXengvSVdQUEpZTUE1";
            var json = File.ReadAllText(@"D:\TFS\LT.Web\Main\Source\com.LoanTek.Framework\com.LoanTek.Framework.UnitTests\com.LoanTek.Quoting\Zillow\request.json");
            
            new ZillowController().MortgageRequestLoadTest(authToken, JsonConvert.DeserializeObject<zillowLoanRequestNotification>(json));
            Thread.Sleep(10000);

            int counter = 200;
            for (int i = 2; i <= counter; i++)
            {
                Task.Run(() =>
                {
                    new ZillowController().MortgageRequestLoadTest(authToken, JsonConvert.DeserializeObject<zillowLoanRequestNotification>(json));
                    Debug.WriteLine("New request @ "+ DateTime.Now);
                });
                Thread.Sleep(r.Next(75, 300));
            }
            int waiter = 0;
            while (true)
            {
                if (waiter++ > counter)
                    break;
                Debug.WriteLine(waiter + " < " + counter + " ... waiting ...");
                Thread.Sleep(500);
            }
        }
    }
}