using com.LoanTek.API.Pricing.Partners.Properties;

namespace com.LoanTek.API.Pricing.Partners
{
    public class DataConnections : IData.DataConnections
    {
        public const IData.DataContextType DataContextType = IData.DataContextType.Database;

        public DataConnections()
        {
            DataContextQuoteSystemsWrite = Settings.Default.DataContextQuoteSystemsWrite ?? DataContextQuoteSystemsWrite;
            DataContextQuoteSystemsRead = Settings.Default.DataContextQuoteSystemsRead ?? DataContextQuoteSystemsRead;
            DataContextLoanTekWrite = Settings.Default.DataContextLoanTekWrite ?? DataContextLoanTekWrite;
            DataContextLoanTekRead = Settings.Default.DataContextLoanTekRead ?? DataContextLoanTekRead;
            DataContextLeadsWrite = Settings.Default.DataContextLeadsWrite ?? DataContextLeadsWrite;
            DataContextLeadsRead = Settings.Default.DataContextLeadsRead ?? DataContextLeadsRead;
            DataContextQuoteDataRead = Settings.Default.DataContextQuoteDataRead ?? DataContextQuoteDataRead;
        }
    }
}