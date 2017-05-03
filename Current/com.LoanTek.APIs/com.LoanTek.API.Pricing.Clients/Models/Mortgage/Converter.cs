namespace com.LoanTek.API.Pricing.Clients.Models.Mortgage
{
    public class Converter : Quoting.Common.Converter
    {
        protected new string GenerateQuoteId(string uniqueId)
        {
            return "LTCQ" + uniqueId;
        }
            
    }
}