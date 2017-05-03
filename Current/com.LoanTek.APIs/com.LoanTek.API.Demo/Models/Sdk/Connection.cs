using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using com.LoanTek.API.Demo.Properties;
using Newtonsoft.Json;

namespace com.LoanTek.API.Demo.Models.Sdk
{
    public interface IConnection
    {
        void UpdateWebServiceBaseAddress(string serverAddress);
        Task<TResult> GetRequest<TResult, TContent>(string endpoint);
        Task<TResult> PostRequest<TApiRequest, TResult, TContent>(string endpoint, TApiRequest request);
        Task<TResult> PostRequestWithStringContent<TApiRequest, TResult>(string endpoint, TApiRequest request);

    }

    public class Connection
    {
        static Connection()
        {
            //some of these are also set in web.config...
            ServicePointManager.DefaultConnectionLimit = 1000;
            ServicePointManager.MaxServicePointIdleTime = 500;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.CheckCertificateRevocationList = false;
        }

        private static string webServiceBaseAddress;

        public static void UpdateWebServiceBaseAddress(string serverAddress)
        {
            if (serverAddress != null && serverAddress == webServiceBaseAddress)
                return;
            webServiceBaseAddress = serverAddress;
            client = null;
        }

        private static HttpClient client;

        private static HttpClient getClient()
        {
            if (client == null)
            {
                if (webServiceBaseAddress == null)
                    webServiceBaseAddress = Settings.Default.WebServiceBaseAddress;

                //client.BaseAddress = new Uri("http://52.55.40.199"); //52.90.207.172:80");
                //client.BaseAddress = new Uri("http://localhost:8887");
                //client.BaseAddress = new Uri("http://partners-pricing-api-2.loantek.com");
                //client.BaseAddress = new Uri("http://10.83.95.25");
                //client.BaseAddress = new Uri("http://localhost:51259");

                Debug.WriteLine("initClient: " + webServiceBaseAddress);

                client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                client.BaseAddress = new Uri(webServiceBaseAddress);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            return client;
        }

        public static async Task<Result<TApiResponse>> GetRequest<TApiResponse>(string endpoint)
        { 
            //Debug.WriteLine(DateTime.Now.ToString("T") + " Posting for to: " + endpoint);
            Stopwatch sw = new Stopwatch();
            sw.Start();
                
            HttpResponseMessage response = await getClient().GetAsync(endpoint);
            Result<TApiResponse> result = new Result<TApiResponse> { HttpStatusCode = response.StatusCode, IsSuccessStatusCode = response.IsSuccessStatusCode };
            if (result.IsSuccessStatusCode)
                result.Content = await response.Content.ReadAsAsync<TApiResponse>(new[] {new JsonMediaTypeFormatter {SerializerSettings = Global.JsonSettings}});
            if (result.Content == null)
            {
                result.ReasonPhrase = response.Content?.ReadAsStringAsync().Result ?? response.ReasonPhrase;
                Debug.WriteLine("failed: " + result.HttpStatusCode + " " + response.ReasonPhrase + " " + response.Content?.ReadAsStringAsync().Result);
            }
            sw.Stop();
            result.TimeInMillisecondsToProcess = sw.ElapsedMilliseconds;
            return result;
        }

        public static async Task<Result<TApiResponse>> PostRequest<TApiRequest, TApiResponse>(string endpoint, TApiRequest request)
        {
            Debug.WriteLine(DateTime.Now.ToString("ss.fff") + " Posting to: " + endpoint);
            Stopwatch sw = new Stopwatch();
            sw.Start(); 
                
            HttpResponseMessage response = await getClient().PostAsJsonAsync(endpoint, request);
            Result<TApiResponse> result = new Result<TApiResponse> { HttpStatusCode = response.StatusCode, IsSuccessStatusCode = response.IsSuccessStatusCode };
            if (result.IsSuccessStatusCode)
                result.Content = await response.Content.ReadAsAsync<TApiResponse>(new[] { new JsonMediaTypeFormatter { SerializerSettings = Global.JsonSettings } });
            if (result.Content == null)
            {
                result.ReasonPhrase = response.Content?.ReadAsStringAsync().Result ?? response.ReasonPhrase;
                Debug.WriteLine("failed: " + result.HttpStatusCode + " " + response.ReasonPhrase + " " + response.Content?.ReadAsStringAsync().Result);
            }
            sw.Stop();
            result.TimeInMillisecondsToProcess = sw.ElapsedMilliseconds;

            return result;
        }

        public static async Task<Result<string>> PostRequestWithStringContent<TApiRequest>(string endpoint, TApiRequest request)
        {
            Debug.WriteLine(DateTime.Now.ToString("ss.fff") + " PostRequest2 to: " + endpoint);
            Stopwatch sw = new Stopwatch(); 
            sw.Start();         

            StringContent content = new StringContent(JsonConvert.SerializeObject(request, Global.JsonSettings), Encoding.UTF8, "application/json");

            var response = await getClient().PostAsync(endpoint, content);
            Result<string> result = new Result<string>{ HttpStatusCode = response.StatusCode, IsSuccessStatusCode = response.IsSuccessStatusCode };
            if (result.IsSuccessStatusCode)
            {
                result.Content = await response.Content.ReadAsStringAsync();
            }
            if (result.Content == null)
            {
                result.ReasonPhrase = response.Content?.ReadAsStringAsync().Result ?? response.ReasonPhrase;
                Debug.WriteLine("failed: " + result.HttpStatusCode + " " + response.ReasonPhrase + " " + response.Content?.ReadAsStringAsync().Result);
            }
            result.TimeInMillisecondsToProcess = sw.ElapsedMilliseconds;
            sw.Stop();

            return result;
        }

        public static void Dispose()
        {
            client?.CancelPendingRequests();
            client?.Dispose();
        }

    }
}