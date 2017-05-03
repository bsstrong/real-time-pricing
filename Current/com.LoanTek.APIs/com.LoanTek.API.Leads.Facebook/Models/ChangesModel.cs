using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace com.LoanTek.API.Leads.Facebook.Models
{
    public class ChangesModel
    {
        public List<Entry> entry { get; set; }
        public string @object { get; set; }

        public class Value
        {
            public string ad_id { get; set; }
            public string form_id { get; set; }
            public string leadgen_id { get; set; }
            public string created_time { get; set; }
            public string page_id { get; set; }
            public string adgroup_id { get; set; }
        }

        public class Change
        {
            public string field { get; set; }
            public Value value { get; set; }
        }

        public class Entry
        {
            public List<Change> changes { get; set; }
            public string id { get; set; }
            public int time { get; set; }
        }
    }
}