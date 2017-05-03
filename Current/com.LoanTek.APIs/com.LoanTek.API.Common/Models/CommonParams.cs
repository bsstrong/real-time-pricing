using System.Linq;
using System.Net.Http;
using com.LoanTek.Quoting.Zillow;
using com.LoanTek.Types;
using LoanTek.Utilities;

namespace com.LoanTek.API.Common.Models
{
    public class CommonParams
    {
        public int TimeoutInMill = 12000;
        public int UseOnlyThisUserId = Global.UseOnlyThisUserId;
        public Processing.DebugModeType DebugModeType = Global.DebugModeType;


        public CommonParams(HttpRequestMessage request)
        {
            if (request?.GetQueryNameValuePairs() == null)
                return;
            var value = request.GetQueryNameValuePairs().FirstOrDefault(x => x.Key == "TimeOutInMill").Value;
            if (value != null)
                TimeoutInMill = NullSafe.NullSafeInteger(value, TimeoutInMill);

            //THIS IS ALSO USED BY ZILLOW 'single user request'
            value = request.GetQueryNameValuePairs().FirstOrDefault(x => x.Key == "UseOnlyThisUserId").Value;
            if (value != null)
                UseOnlyThisUserId = NullSafe.NullSafeInteger(value, UseOnlyThisUserId);

            value = request.GetQueryNameValuePairs().FirstOrDefault(x => x.Key == "DebugModeType").Value;
            if (value != null)
                EnumLib.TryParse(value, out DebugModeType);
        }

    }
}