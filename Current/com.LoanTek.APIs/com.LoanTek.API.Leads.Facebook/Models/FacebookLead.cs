using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using LoanTek.BusinessObjects.Leads;

namespace com.LoanTek.API.Leads.Facebook.Models
{
    public class FacebookLead
    {
        public DateTime Created { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }

        public MortgageLead ToMortgageLead(int clientID, bool saveToDB = false)
        {
            string[] names = (FullName + " ").Split(' ');

            var loanTekLead = new MortgageLead()
            {
                FullName = FullName,
                FirstName = names[0],
                LastName = names[1],

                MailCity = City,
                MailState = State,
                MailZip = Zip,

                ClientID = clientID,
                SourceCreated = Created,
                Created = DateTime.Now
            };

            if (saveToDB)
                loanTekLead.Save();

            return loanTekLead;
        }

    }
}