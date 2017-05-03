using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using com.LoanTek.Forms.Mortgage;
using com.LoanTek.Quoting.Mortgage;
using com.LoanTek.Quoting.Mortgage.Common;
using QuotingUser = com.LoanTek.Quoting.Mortgage.PreQualUsers.QuotingUser;
// ReSharper disable StaticMemberInGenericType - message disabled because having a static member in this generic type is acceptable behavior for this class

namespace com.LoanTek.API.Demo.Models.Sdk
{
    public class QuoteMortgageRequest<TApiRequest, TApiResponse> where TApiRequest : MortgageRequest, new() where TApiResponse : MortgageResponse<IMortgageSubmission<IMortgageQuote>, IMortgageQuote>
    {
        public ConcurrentQueue<IMortgageSubmission<IMortgageQuote>> Submissions;
        public ConcurrentBag<Connection.Result<TApiResponse>> Results = new ConcurrentBag<Connection.Result<TApiResponse>>();   

        protected static List<QuotingUser> QuotingUsers;

        private DateTime waitUntilAfter;
        private static readonly SemaphoreSlim awaitLock = new SemaphoreSlim(1, 1);
        public async Task<List<QuotingUser>> GetPreQualUsers()  
        {
            if (DateTime.Now > waitUntilAfter)
            {
                await awaitLock.WaitAsync(); //only one at a time..
                {
                    if (DateTime.Now > waitUntilAfter)
                    {
                        try
                        {
                            waitUntilAfter = DateTime.Now.AddMinutes(1);
                            Connection.Result<List<QuotingUser>> result = await Connection.GetRequest<List<QuotingUser>>("Bankrate/VWRsL3FrVllETEZFbnlocjZFbEJUcHRvOU1NRG11eGx3N0NZeW84YjRZWE1vc3VOY0cwdkYxMXZKWkRZU0tHc2c1VUd2MG9nMmI0QQ2/PreQualList");
                            if (result?.Content != null)
                                QuotingUsers = result.Content;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("ERROR @ GetPreQualUsers:"+ ex.Message);
                        }
                        finally { awaitLock.Release(); }    
                    }
                }
            }
            return QuotingUsers;
        }
            
        public virtual async Task<ConcurrentBag<Connection.Result<TApiResponse>>> Quote(IMortgageForm obj, bool useTestEndPoint = false)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            TApiRequest request = new TApiRequest();
            request.Form = (ConsumerMortgageLoanForm) obj;

            try
            {
                await Task.Run(() =>
                {
                    Parallel.ForEach(QuotingUsers, async quotingUser =>
                    {
                        var result = useTestEndPoint ? 
                            await Connection.GetRequest<TApiResponse>("Bankrate/VWRsL3FrVllETEZFbnlocjZFbEJUcHRvOU1NRG11eGx3N0NZeW84YjRZWE1vc3VOY0cwdkYxMXZKWkRZU0tHc2c1VUd2MG9nMmI0QQ2/FullMortgageRequest/Test?UseOnlyThisUserId=" + quotingUser.UserId) : 
                            await Connection.PostRequest<TApiRequest, TApiResponse>("Bankrate/VWRsL3FrVllETEZFbnlocjZFbEJUcHRvOU1NRG11eGx3N0NZeW84YjRZWE1vc3VOY0cwdkYxMXZKWkRZU0tHc2c1VUd2MG9nMmI0QQ2/FullMortgageRequest?UseOnlyThisUserId=" + quotingUser.UserId, request);
                        if (result?.Content?.Submissions?.FirstOrDefault() != null)
                            this.Submissions.Enqueue(result.Content.Submissions.FirstOrDefault());
                        Results.Add(result);
                    });
                });
                Debug.WriteLine("TOTAL TIME TO POST ALL -in milli: " + sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR:" + ex.Message);
            }
            sw.Stop();
            return Results;     
        }

        public virtual IMortgageSubmission<IMortgageQuote> GetSubmission()
        {
            IMortgageSubmission<IMortgageQuote> submission;
            this.Submissions.TryDequeue(out submission);
            return submission;
        }
    }
}