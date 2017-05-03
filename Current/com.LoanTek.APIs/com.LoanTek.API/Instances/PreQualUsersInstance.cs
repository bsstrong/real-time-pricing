using System;
using System.Collections.Generic;
using com.LoanTek.Quoting;
using com.LoanTek.Types;
#pragma warning disable 1591

namespace com.LoanTek.API.Instances
{
    public class PreQualUsersInstance : IInstance
    {
        public PreQualUsersInstance() { }

        public PreQualUsersInstance(string uri) : base(uri)
        {
            this.ActivePreQualUsers = new List<PreQualUser>();
            this.ActiveChannelQuotingUsers = new List<QuotingUser>();
            this.ActiveChannelScheduledQuotingUsers = new List<QuotingUser>();
        }

        public PreQualUsersInstance(QuotingChannelType quotingChannelType, IPreQualUsers iPreQual, string uri) : base(uri)
        {
            var allPreQualUsers = iPreQual.GetPreQualUsersCopy();
            this.ActivePreQualUsers = iPreQual.FilterByActivePartnerPreference(allPreQualUsers);
            this.ActiveChannelQuotingUsers = iPreQual.FilterByQuotingChannel(ActivePreQualUsers, quotingChannelType);
            this.ActiveChannelScheduledQuotingUsers = iPreQual.FilterByScheduledDayTime(new List<QuotingUser>(this.ActiveChannelQuotingUsers));
            this.UpdateDataCheckInterval = iPreQual.UpdateDataCheckInterval;
            this.IsDataUpdateRunning = iPreQual.IsDataUpdateRunning;
        }
        
        public List<PreQualUser> ActivePreQualUsers { get; set; }
        public List<QuotingUser> ActiveChannelQuotingUsers { get; set; }
        public List<QuotingUser> ActiveChannelScheduledQuotingUsers { get; set; }
        public TimeSpan? UpdateDataCheckInterval { get; set; }
        public bool IsDataUpdateRunning { get; set; }
    }
}