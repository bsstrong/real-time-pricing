using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using com.LoanTek.API.Demo.Models.Sdk;
using com.LoanTek.Quoting.Mortgage.PreQualUsers;

namespace com.LoanTek.API.Demo.Models.Bankrate.Mortgage
{
    public static class PreQualUsers
    {
        public static List<QuotingUser> QuotingUsers;

        private static DateTime waitUntilAfter;
        private static readonly SemaphoreSlim awaitLock = new SemaphoreSlim(1, 1);
        public static async Task<List<QuotingUser>> UpdatePreQualUsers()
        {
            if (QuotingUsers == null || DateTime.Now > waitUntilAfter)
            {
                await awaitLock.WaitAsync(); //only one at a time..
                {
                    if (QuotingUsers == null || DateTime.Now > waitUntilAfter)
                    {
                        try
                        {
                            waitUntilAfter = DateTime.Now.AddMinutes(1);
                            Result<List<QuotingUser>> result = await Connection.GetRequest<List<QuotingUser>>("Bankrate/VWRsL3FrVllETEZFbnlocjZFbEJUcHRvOU1NRG11eGx3N0NZeW84YjRZWE1vc3VOY0cwdkYxMXZKWkRZU0tHc2c1VUd2MG9nMmI0QQ2/PreQualList");
                            if (result?.Content != null)
                                QuotingUsers = result.Content;
                            Debug.WriteLine("GetPreQualUsers result:" + result?.HttpStatusCode + " in " + result?.TimeInMillisecondsToProcess + " count:" + result?.Content?.Count);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("ERROR @ GetPreQualUsers:" + ex.Message);
                        }
                        finally { awaitLock.Release(); }
                    }
                }
            }
            return QuotingUsers;
        }
    }
}