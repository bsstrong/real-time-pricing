using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Helpers;
using com.LoanTek.API.Common.Models;
using com.LoanTek.Quoting;
using com.LoanTek.Quoting.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

//using com.LoanTek.API.Pricing.Mortgage.Areas.HelpPage.ModelDescriptions;

namespace com.LoanTek.API.Pricing.Clients.Models.Common.Mortgage
{
    /// <summary>
    /// 
    /// </summary>
    public class CustomMortgageResponse : FullMortgageResponse
    {
        public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings();

        static CustomMortgageResponse()
        {
            JsonSettings.NullValueHandling = NullValueHandling.Ignore;
            JsonSettings.Formatting = Formatting.None;
            JsonSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
            JsonSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        }

        /// <summary>
        /// Converts the list of IQuotes into a List of objects based on the CustomLoanResponseJson. 
        /// </summary>
        /// <param name="customLoanResponseJson"></param>
        /// <param name="quotes"></param>
        /// <returns>List of dynamic objects based on the passed in CustomLoanResponseJson</returns>
        /// <exception cref="Exception"></exception>
        public List<MortgageLoanQuote> ConvertCustomJsonToLoanResponse(string customLoanResponseJson, List<IQuote> quotes)
        {
            JObject obj;
            try
            {
                obj = JObject.Parse(customLoanResponseJson);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to convert custom json string to Loan Response object: " + ex.Message);
            }
            try
            {
                this.Quotes = new List<MortgageLoanQuote>();

                var sourceType = typeof (Quote);

                foreach (var rate in quotes)
                {
                    foreach (var item in obj)
                    {
                        //Debug.WriteLine("key:" + item.Key);
                        var sourceProperty = sourceType.GetProperty(item.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                        object valueToSet = sourceProperty?.GetGetMethod().Invoke(rate, null);
                        if (valueToSet == null)
                            continue;
                        if (item.Value.HasValues)
                        {
                            JObject token = new JObject();
                            foreach (var item2 in (JObject) item.Value)
                            {
                                //Debug.WriteLine(" nested key: " + item2.Key);
                                var sourceProperty2 = valueToSet.GetType().GetProperty(item2.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                                object valueToSet2 = sourceProperty2?.GetGetMethod().Invoke(valueToSet, null);
                                if (valueToSet2 == null)
                                    continue;
                                token[item2.Key] = JToken.FromObject(valueToSet2);
                            }
                            valueToSet = token;
                        }
                        obj[item.Key] = JToken.FromObject(valueToSet);
                    }
                    //var json = JsonConvert.SerializeObject(obj);
                    //Debug.WriteLine("response json obj:\n" + json);
                    this.Quotes.Add(Json.Decode(JsonConvert.SerializeObject(obj, JsonSettings)));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to add data to custom response object: " + ex.Message);
            }
            return this.Quotes;
        }
    }
    
}