using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace com.LoanTek.API.Leads.Facebook.Controllers
{
    public class Wrapper : ApiController
    {
        public Wrapper()
        { }

        /// <summary>
        /// This holds all the Query or Post params passed to the web service or method 
        /// </summary>
        public IDictionary<string, string> TheParams;
        /// <summary>
        /// This parses the Request to populate the <see cref="TheParams"/> object and set other fields needed by any child class 
        /// </summary>
        /// <param name="request"></param>
        /// <returns>A Dictionary list of all the passed in params</returns>
        public IDictionary<string, string> GetParams(HttpRequestMessage request)
        {
            var m = request.GetQueryNameValuePairs();
            if (m.Any())
            {
                var sss = string.Empty;
            }
            TheParams = request.GetQueryNameValuePairs().GroupBy(x => x.Key).Select(x => x.First()).ToDictionary(x => x.Key.ToLower(), x => x.Value);
            foreach (var key in TheParams.Keys)
            {
                Debug.WriteLine("-param: " + key + "=" + TheParams[key]);
            }
            return TheParams;
        }

        /// <summary>
        /// Gets the value of a single passed in query or post param
        /// </summary>
        /// <param name="name">The name of the query or post param</param>
        /// <returns>The value of the param name</returns>
        public string GetParam(string name)
        {
            return TheParams != null ? TheParams.FirstOrDefault(x => x.Key == name.ToLower()).Value : string.Empty;
        }

        protected bool HasNeededParams(string[] theParams)
        {
            return !theParams.Any(param => string.IsNullOrEmpty(this.GetParam(param)));
        }
    }
}