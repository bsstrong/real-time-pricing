using System;
using System.Collections.Generic;
using System.Linq;
using com.LoanTek.API.Pricing.Partners.Models.Mortgage;
using com.LoanTek.Forms.Mortgage;
using com.LoanTek.Master.Data.LinqDataContexts;
using com.LoanTek.Types;
using LoanTek.Pricing.LoanRequests;
using LoanTek.Utilities;

namespace com.LoanTek.API.Pricing.Partners.Models
{
    /// <summary>
    /// Class for generating or querying dummy or test data
    /// </summary>
    public class DummyData
    {
        public static FullMortgageRequest CreateDummyRequest(QuotingChannelType quotingChannelType)
        {
            FullMortgageRequest dummyRequest = new FullMortgageRequest();
            dummyRequest.PassThroughItems = new List<object>() { new { Item = "A Pass Through Item" } };
            dummyRequest.Form = new ConsumerMortgageLoanForm();
            ClassMappingUtilities.SetPropertiesForTarget(DummyData.GetRequest(), dummyRequest.Form);
            dummyRequest.Form.Amount = 300000;
            dummyRequest.Form.LoanPurposeType = LoanPurposeType.Purchase;
            dummyRequest.Form.PropertyTypeType = PropertyTypeType.SingleFamily;
            dummyRequest.Form.PropertyUseType = PropertyUseType.PrimaryResidence;
            dummyRequest.Form.LockPeriodType = LockPeriodType.ClosestTo30;
            dummyRequest.Form.BestExecutionMethodType = BestExecutionMethodType.ByPointGroup;
            dummyRequest.ClientDefinedId = "LT-Test" + StringUtilities.UniqueId();
            return dummyRequest;
        }

        private static List<MortgageLoanRequest> requests;
        public static MortgageLoanRequest GetRequest()
        {
            Random r = new Random();
            if (requests == null || requests.Count < 2)
            {
                requests = new List<MortgageLoanRequest>();
                using (QuotingDataContext dc = new QuotingDataContext(Partners.DataConnections.DataContextQuoteDataRead))
                {
                    var results = dc.MortgageQuote_Requests.OrderByDescending(x => x.Id).Skip(r.Next(50, 1000)).Where(x => x.UsersQuotedCount > 0 && x.LoanRequest != null).Select(x => x.LoanRequest).Take(500);
                    results.ForEach(x => requests.Add(SqlUtilities.JsonDecompressConverter<MortgageLoanRequest>(x)));
                }
            }
            if (requests.Count < 2)
            {
                using (QuotingDataContext dc = new QuotingDataContext(Partners.DataConnections.DataContextQuoteDataRead))
                {
                    var results = dc.MortgageQuote_Requests.OrderByDescending(x => x.Id).Skip(r.Next(20, 100)).Where(x => x.UsersQuotedCount > 0 && x.LoanRequest != null).Select(x => x.LoanRequest).Take(500);
                    results.ForEach(x => requests.Add(SqlUtilities.JsonDecompressConverter<MortgageLoanRequest>(x)));
                }
            }
            return requests[r.Next(0, requests.Count - 1)];
        }

        private static Dictionary<int, List<MortgageLoanRequest>> requestsByUserId;
        public static MortgageLoanRequest GetRequest(int userId)
        {
            if (userId == 0)
                return GetRequest();
            Random r = new Random();
            if (requestsByUserId == null || !requestsByUserId.ContainsKey(userId))
            {
                requestsByUserId = new Dictionary<int, List<MortgageLoanRequest>>();
                using (QuotingDataContext dc = new QuotingDataContext(Partners.DataConnections.DataContextQuoteDataRead))
                {
                    var loanRequests = new List<MortgageLoanRequest>();
                    var ids = dc.MortgageQuote_Submissions.OrderByDescending(x => x.Id).Skip(r.Next(50, 1000)).Where(x => x.UserId == userId && x.Status == Processing.StatusType.Complete.ToString()).Take(5).Select(x => x.ParentId);
                    if (ids.Any())
                    {
                        foreach (var parentId in ids)
                        {
                            var result = dc.MortgageQuote_Requests.FirstOrDefault(x => x.Id == parentId);
                            if(result?.LoanRequest != null)
                                loanRequests.Add(SqlUtilities.JsonDecompressConverter<MortgageLoanRequest>(result.LoanRequest));
                        }
                    }
                    requestsByUserId.Add(userId, loanRequests);
                }   
            }
            var data = requestsByUserId[userId];
            return (data.Count > 0) ? data[r.Next(0, data.Count - 1)] : GetRequest();
        }
    }
}