
using System;
using com.LoanTek.API.Leads.Clients.Properties;

namespace com.LoanTek.API.Leads.Clients
{
    public class DataConnections : IData.DataConnections
    {
        public static string DataContextBankrateRead;
        public static string DataContextBankrateWrite;

        public DataConnections()
        {
            DataContextLoanTekWrite = Settings.Default.DataContextLoanTekWrite ?? DataContextLoanTekWrite;
            DataContextLoanTekRead = Settings.Default.DataContextLoanTekRead ?? DataContextLoanTekRead;
            DataContextLeadsWrite = Settings.Default.DataContextLeadsWrite ?? DataContextLeadsWrite;
            DataContextLeadsRead = Settings.Default.DataContextLeadsRead ?? DataContextLeadsRead;
            DataContextBankrateRead = Settings.Default.DataContextBankrateRead;
            DataContextBankrateWrite = Settings.Default.DataContextBankrateWrite;
        }
    }
}