using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using com.LoanTek.API.Filters;
using com.LoanTek.API.Pricing.Clients.Models;
using com.LoanTek.API.Pricing.Clients.Models.Common.Mortgage;
using com.LoanTek.Master;
using LoanTek.LoggingObjects;
using WebApiThrottle;
using IQuote = com.LoanTek.Quoting.IQuote;

namespace com.LoanTek.API.Pricing.Clients.Controllers
{
    [EnableThrottling(PerSecond = 1, PerMinute = 10)] //enables Rate Limits
    [PreProcessFilter] //does general PRE-processing for a web service call
    [PostProcessFilter] //does general POST-processing for a web service call
    [RoutePrefix("Test/{authToken}/{userId}")]
    public class TestController : AApiController
    {
        public const string ApiName = "Pricing/Clients/Test/"; //this is important because the {authToken} uses this as part of its ApiName

        private static readonly Logger errorLogger = new Logger(SimpleLogger.LogToType.EMAIL);
        private static readonly Logger requestLogger = new Logger(SimpleLogger.LogToType.FILE);

        static TestController() //A static constructor is used to initialize any static data, or to perform a particular action that needs performed once only
        {
            if (string.IsNullOrEmpty(requestLogger.FileDefault))
                requestLogger.CreateLogFile("TestController.txt");
        }

        private readonly SimpleLogger.LocationObject locationObject;

        public TestController()
        {
            locationObject = new SimpleLogger.LocationObject(this, null);
        }

        [Route("Get")]
        [HttpGet]
        public HttpResponseMessage Get(string authToken, int userId)
        {
            //Debug.WriteLine("authToken:" + authToken);
            string apiEndPoint = ApiName + ".Get/GET";

            FullMortgageResponse response = new FullMortgageResponse();
            //response.ApiEndPoint = "FullMortgageRequest";
            try
            {
                //response.ClientDefinedIdentifier = this.Request.GetQueryNameValuePairs().FirstOrDefault(x => x.Key.ToLower().Equals("clientdefinedidentifier")).Value ?? "NONE";
                //response.LoanTekDefinedIdentifier = this.Request.Properties["UniqueId"] as string;
                //response.ApiEndPoint = this.Request.RequestUri.AbsolutePath;

                if (string.IsNullOrEmpty(authToken))
                    return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid or missing required data", response);
                
                AuthToken obj = new AuthToken(authToken);
                if (string.IsNullOrEmpty(obj.ApiName) || obj.ClientId < 1)
                    return this.createErrorResponse(HttpStatusCode.BadRequest, "Invalid or missing required data (AuthToken is invalid)", response);
                if (!obj.ApiName.Equals(ApiName +"Get"))
                    return this.createErrorResponse(HttpStatusCode.Unauthorized, "Invalid Authentication Token", response);

                AClient client = Master.Clients.GetClientById(obj.ClientId);
                if (client == null || !client.Active)
                    return this.createErrorResponse(HttpStatusCode.Forbidden, "Client is invalid or not active", response);

                //TODO other checks go here
                //if(userId < 1 || )

                Random random = new Random();
                response.Quotes = new List<IQuote>();
                for (int i = 0; i < random.Next(0, 20); i++)
                {
                    response.Quotes.Add(new Quote());
                    Thread.Sleep(random.Next(10, 200));
                }

                HttpResponseMessage httpResponse = Request.CreateResponse(HttpStatusCode.OK, response);
                return httpResponse;
            }
            catch (Exception ex)
            {
                errorLogger.Log(SimpleLogger.LogLevelType.ERROR, ex.Message, this.locationObject);
                return this.createErrorResponse(HttpStatusCode.InternalServerError, "System error. Please contact support.", response);
            }
            
        }
    }
}
