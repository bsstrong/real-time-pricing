using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using com.LoanTek.Types;
using LoanTek.Utilities;

namespace com.LoanTek.API.Instances
{
    public class IInstance
    {
        //No args constructor Need for Json serialization
        public IInstance() { }

        public IInstance(string uri)
        {
            this.Uri = uri ?? "NA";
            this.Ips = new List<string>();
            Dns.GetHostEntry(Dns.GetHostName())?.AddressList.ForEach(x => Ips.Add(x.ToString()));
        }
        public string Uri { get; set; }
        public string EndPoint { get; set; }    
        public string MachineName { get; set; } = Environment.MachineName;
        public List<string> Ips { get; set; }
    }
}   