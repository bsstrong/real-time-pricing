using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using com.LoanTek.API.Demo.Models.Sdk;
using com.LoanTek.Forms.Mortgage;
using Newtonsoft.Json;
using MortgageRequest = com.LoanTek.API.Pricing.Partners.Models.Mortgage.FullMortgageRequest;
using MortgageResponse = com.LoanTek.API.Pricing.Partners.Models.Mortgage.FullMortgageResponse<com.LoanTek.Quoting.Mortgage.Common.MortgageSubmission<com.LoanTek.Quoting.Mortgage.Bankrate.MortgageQuote>, com.LoanTek.Quoting.Mortgage.Bankrate.MortgageQuote>;

namespace com.LoanTek.API.Demo.Models.Bankrate.Mortgage
{
    public class QuoteRequestStringJsonHybrid : QuoteRequestWithJsonContent
    {
        public new async Task<ConcurrentBag<Model>> Quote(ConsumerMortgageLoanForm form, string clientDefinedId, int userId = 0, bool useTestEndPoint = false, Stopwatch sw = null)
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

                //Parallel.ForEach(QuotingUsers, async quotingUser =>
                var tasks = quotingUsers.Select(async quotingUser =>
                {
                    request.ClientDefinedId += counter++;
                    try
                    {
                        this.RequestsSentCount++;
                        Result<string> result =
                            await Connection.PostRequestWithStringContent(
                                "Bankrate/VWRsL3FrVllETEZFbnlocjZFbEJUcHRvOU1NRG11eGx3N0NZeW84YjRZWE1vc3VOY0cwdkYxMXZKWkRZU0tHc2c1VUd2MG9nMmI0QQ2/FullMortgageRequest?UseOnlyThisUserId=" +
                                quotingUser.UserId, request);
                        this.RequestsCompletedCount++;
                        Model model = new Model
                        {
                            HttpStatusCode = result.HttpStatusCode,
                            IsSuccessStatusCode = result.IsSuccessStatusCode,
                            TimeInMillisecondsFromSubmit = sw.ElapsedMilliseconds,
                            TimeInMillisecondsToProcess = result.TimeInMillisecondsToProcess
                        };

                        if (result.Content != null)
                        {
                            var mortgageResponse = JsonConvert.DeserializeObject<MortgageResponse>(result.Content, Global.JsonSettings);
                            var submission = mortgageResponse?.Submissions?.FirstOrDefault();

                            //Debug.WriteLine(sw.ElapsedMilliseconds + " result: " + result.HttpStatusCode + " #" + submission?.Quotes?.Count);

                            if (submission != null)
                            {
                                model.PricingEngineVersionType = submission.PricingEngineVersionType;
                                model.RequestTimeObjects = mortgageResponse.RequestTimeObjects;
                                model.SubmissionTimeObjects = mortgageResponse.SubmissionTimeObjects;
                                model.TimeInMillisecondsToProcessServer = mortgageResponse.ExecutionTimeInMillisec;
                                model.IsCached = !string.IsNullOrEmpty(submission.CachedId);
                                model.UserId = submission.QuotingUser.UserId;
                                model.Content = submission.Quotes;
                            }
                        }
                        this.ModelQueue.Enqueue(model);
                        this.Results.Add(model);
                    }
                    catch (TaskCanceledException)
                    {
                        this.RequestsCanceledCount++;
                        Debug.WriteLine("TaskCanceledException @ "+ sw.ElapsedMilliseconds);
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
            sw.Stop();
            this.TotalTime = sw.ElapsedMilliseconds;
            this.Finished = true;
            return this.Results;
        }

    }
}