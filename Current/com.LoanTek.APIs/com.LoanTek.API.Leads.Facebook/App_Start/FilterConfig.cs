using System.Web;
using System.Web.Mvc;

namespace com.LoanTek.API.Leads.Facebook
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
