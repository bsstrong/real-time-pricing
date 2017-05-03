using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using com.LoanTek.API.Demo.Models.Sdk;
using com.LoanTek.Forms.Mortgage;
using com.LoanTek.Types;
using QuotingUser = com.LoanTek.Quoting.Mortgage.PreQualUsers.QuotingUser;
using MortgageRequest = com.LoanTek.API.Pricing.Partners.Models.Mortgage.FullMortgageRequest;
using MortgageResponse = com.LoanTek.API.Pricing.Partners.Models.Mortgage.FullMortgageResponse<com.LoanTek.Quoting.Mortgage.Common.MortgageSubmission<com.LoanTek.Quoting.Mortgage.Bankrate.MortgageQuote>, com.LoanTek.Quoting.Mortgage.Bankrate.MortgageQuote>;

namespace com.LoanTek.API.Demo.Models.Bankrate.Mortgage
{
    public class QuoteRequestWithJsonContent
    { 
        protected ConcurrentBag<Model> Results = new ConcurrentBag<Model>();
        protected readonly ConcurrentQueue<Model> ModelQueue = new ConcurrentQueue<Model>();
        public bool Finished { get; set; }
        public int RequestsSentCount { get; set; }
        public int RequestsCompletedCount { get; set; }
        public int RequestsCanceledCount { get; set; }
        public int RequestsErrorCount { get; set; }
        public double TotalTime { get; set; }
        public double AvgTotalTimePerRequest => this.RequestsCompletedCount > 0 ? Math.Round(this.TotalTime / this.RequestsCompletedCount, 4) : -1;
        public double AvgTotalTimePerRequestWithQuotes => this.RequestsCompletedCount > 0 ? Math.Round(this.TotalTime / this.Results.Count(x => x.Content?.Count > 0), 4) : -1;

        public async Task<ConcurrentBag<Model>> Quote(ConsumerMortgageLoanForm form, string clientDefinedId, int userId = 0, bool useTestEndPoint = false, Stopwatch sw = null)
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
                        var result = useTestEndPoint
                            ? await Connection.GetRequest<MortgageResponse>(
                                "Bankrate/VWRsL3FrVllETEZFbnlocjZFbEJUcHRvOU1NRG11eGx3N0NZeW84YjRZWE1vc3VOY0cwdkYxMXZKWkRZU0tHc2c1VUd2MG9nMmI0QQ2/FullMortgageRequest/Test?UseOnlyThisUserId=" +
                                quotingUser.UserId)
                            : await Connection.PostRequest<MortgageRequest, MortgageResponse>(
                                "Bankrate/VWRsL3FrVllETEZFbnlocjZFbEJUcHRvOU1NRG11eGx3N0NZeW84YjRZWE1vc3VOY0cwdkYxMXZKWkRZU0tHc2c1VUd2MG9nMmI0QQ2/FullMortgageRequest?UseOnlyThisUserId=" +
                                quotingUser.UserId, request);
                        this.RequestsCompletedCount++;
                        Model model = new Model
                        {
                            HttpStatusCode = result.HttpStatusCode,
                            IsSuccessStatusCode = result.IsSuccessStatusCode,
                            ReasonPhrase = result.ReasonPhrase,
                            TimeInMillisecondsFromSubmit = sw.ElapsedMilliseconds,
                            TimeInMillisecondsToProcess = result.TimeInMillisecondsToProcess
                        };
                        //Debug.WriteLine(sw.ElapsedMilliseconds +" result: " + result.HttpStatusCode +" #"+ result.Content?.Submissions?.FirstOrDefault()?.Quotes?.Count);

                        var submission = result.Content?.Submissions?.FirstOrDefault();
                        if (submission != null)
                        {
                            model.PricingEngineVersionType = submission.PricingEngineVersionType;
                            model.RequestTimeObjects = result.Content?.RequestTimeObjects;
                            model.SubmissionTimeObjects = result.Content?.SubmissionTimeObjects;
                            model.TimeInMillisecondsToProcessServer = result.Content.ExecutionTimeInMillisec;
                            model.IsCached = !string.IsNullOrEmpty(submission.CachedId);
                            model.UserId = submission.QuotingUser.UserId;
                            model.Content = submission.Quotes;
                        }
                        this.ModelQueue.Enqueue(model);
                        this.Results.Add(model);
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
            return this.Results;
        }

        public Model GetResult()
        {
            Model obj;
            this.ModelQueue.TryDequeue(out obj);
            return obj; 
        }

        protected async Task<List<QuotingUser>> GetQuotingUsers(int userId, MortgageRequest request)
        {
            if (PreQualUsers.QuotingUsers == null)
                await PreQualUsers.UpdatePreQualUsers();
            if (PreQualUsers.QuotingUsers == null || PreQualUsers.QuotingUsers.Count == 0)
                return null;

            //make a copy of the (in case it is updated via UpdatePreQualUsers() at the same time it is being used here...) and filter by userId
            return Quoting.Mortgage.Bankrate.PreQualUsers.Get(request, PreQualUsers.QuotingUsers.ToList(), userId);
        }

        protected MortgageRequest CreateRequest(ConsumerMortgageLoanForm obj, string clientDefinedId)
        {
            MortgageRequest request = new MortgageRequest();
            request.UserId = 0; //do not set the userId here since we want to reuse this request object for every quoting user...
            request.ClientDefinedId = clientDefinedId;
            request.Form = obj;
            request.Form.BestExecutionMethodType = BestExecutionMethodType.ByPointGroup;
            if (request.Form.ProductFamilyTypes == null)
                request.Form.ProductFamilyTypes = new List<ProductFamilyType> { ProductFamilyType.CONVENTIONAL };
            if (request.Form.ProductTermTypes == null)
                request.Form.ProductTermTypes = new List<ProductTermType> { ProductTermType.A5_1, ProductTermType.F15, ProductTermType.F30 };
            if (request.Form.QuoteTypesToReturn == null)
                request.Form.QuoteTypesToReturn = new List<QuoteTypeType> { QuoteTypeType.ClosestToZeroNoFee, QuoteTypeType.ClosestToZeroWithFee, QuoteTypeType.ClosestTo01, QuoteTypeType.ClosestTo02 };
            request.PassThroughItems = new List<object> { "ShowTimeObjects" };

            if (request.HasErrors() != null)
                throw new Exception("Form has errors:" + string.Join("|", request.HasErrors()));

            return request;
        }
       
    }
}