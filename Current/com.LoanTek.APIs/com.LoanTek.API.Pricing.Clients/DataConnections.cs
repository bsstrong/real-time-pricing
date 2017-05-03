using com.LoanTek.API.Pricing.Clients.Properties;

namespace com.LoanTek.API.Pricing.Clients
{
    public class DataConnections : API.Pricing.Partners.DataConnections
    {
        public DataConnections()
        {
            DataContextQuoteDataRead = Settings.Default.DataContextQuoteDataRead ?? DataContextQuoteDataRead;
        }
    }
}