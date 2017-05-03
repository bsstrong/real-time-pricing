using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using com.LoanTek.Types;
using LoanTek.Pricing.BusinessObjects;
using Newtonsoft.Json;

namespace com.LoanTek.API.Requests
{
    /// <summary>
    /// Contains the members and data points necessary to price a mortgage request with a full set of requirements
    /// </summary>
    /// <remarks>
    /// Overwrites the MortgageLoanRequest object, removing or 'hiding' unneeded properties. 
    /// </remarks>
    public class MortgageLoanRequest : global::LoanTek.Pricing.LoanRequests.MortgageLoanRequest
    {
        //[XmlIgnore] [JsonIgnore] [IgnoreDataMember] public override QuotingChannelType QuotingChannel   { get; set; }
        [XmlIgnore] [JsonIgnore] [IgnoreDataMember] public override string ClientDefinedIdentifier { get; set; }
        [XmlIgnore] [JsonIgnore] [IgnoreDataMember] public override ACounty County { get; set; }
        [XmlIgnore] [JsonIgnore] [IgnoreDataMember] public override string CountyName { get; set; }
        [XmlIgnore] [JsonIgnore] [IgnoreDataMember] public override string StateAbbreviation { get; set; }
        [XmlIgnore] [JsonIgnore] [IgnoreDataMember] public override bool IsValid => base.IsValid;
        [XmlIgnore] [JsonIgnore] [IgnoreDataMember] public override List<string> ValidationErrors => base.ValidationErrors;
    }
}
