using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using com.LoanTek.API.Demo.Models.Sdk;
using com.LoanTek.Forms.Mortgage;
using MortgageRequest = com.LoanTek.API.Pricing.Partners.Models.Mortgage.FullMortgageRequest;

namespace com.LoanTek.API.Demo.Models.Bankrate.Mortgage
{
    public class QuoteRequestWithStringContent : QuoteRequestWithJsonContent
    {
        protected new readonly ConcurrentBag<string> Results = new ConcurrentBag<string>();
        private readonly ConcurrentQueue<string> modelQueue = new ConcurrentQueue<string>();

        public new async Task<ConcurrentBag<string>> Quote(ConsumerMortgageLoanForm form, string clientDefinedId, int userId = 0, bool useTestEndPoint = false, Stopwatch sw = null)
        {
            if (sw == null)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            int counter = 1;
            try
            {
                MortgageRequest request = this.CreateRequest(form, clientDefinedId);

                var quotingUsers = this.GetQuotingUsers(userId, request).Result;
                //Debug.WriteLine("quotingUsers count before:" + PreQualUsers.QuotingUsers.Count + " -after:" + quotingUsers.Count);

                //Parallel.ForEach(QuotingUsers, async quotingUser =>
                var tasks = quotingUsers.Select(async quotingUser =>
                {
                    request.ClientDefinedId += counter++;
                    try
                    {
                        this.RequestsSentCount++;
                        Result<string> result = await Connection.PostRequestWithStringContent("Bankrate/VWRsL3FrVllETEZFbnlocjZFbEJUcHRvOU1NRG11eGx3N0NZeW84YjRZWE1vc3VOY0cwdkYxMXZKWkRZU0tHc2c1VUd2MG9nMmI0QQ2/FullMortgageRequest?UseOnlyThisUserId=" + quotingUser.UserId, request);
                        this.RequestsCompletedCount++;

                        string model = "{ \"HttpStatusCode\" : \""+ result.HttpStatusCode + "\", \"IsSuccessStatusCode\" : \"" + result.IsSuccessStatusCode + "\", \"ReasonPhrase\" : \"" + result.ReasonPhrase + "\", \"TimeInMillisecondsFromSubmit\": " + sw.ElapsedMilliseconds + ", \"TimeInMillisecondsToProcess\": " + result.TimeInMillisecondsToProcess + "," + result.Content.Substring(1);

                        this.modelQueue.Enqueue(model);
                        Results.Add(model);
                    }
                    catch (TaskCanceledException)
                    {
                        this.RequestsCanceledCount++;
                        Debug.WriteLine("TaskCanceledException @ " + sw.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        this.RequestsErrorCount++;
                        Debug.WriteLine("ERROR:" + ex.Message);
                    }
                }); 
                await Task.WhenAll(tasks);

                Debug.WriteLine("Quote TOTAL TIME -in milli: " + sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR:" + ex.Message);
            }
            this.TotalTime = sw.ElapsedMilliseconds;
            this.Finished = true;
            return Results;
        }

        public new string GetResult()
        {
            string obj;
            this.modelQueue.TryDequeue(out obj);
            return obj;     
        }

    }
}