using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Xml.Serialization;
using com.LoanTek.API.Leads.Clients.Models;
using com.LoanTek.API.Leads.Clients.Models.Common;
using com.LoanTek.Biz.Contacts.Leads.Actions;
using com.LoanTek.Biz.Contacts.Leads.Objects;
using com.LoanTek.IData;
using com.LoanTek.Master;
using LoanTek.Schemas;
using LoanTek.Utilities;
using StatusType = com.LoanTek.CRM.Processing.StatusType;
using Users = com.LoanTek.Master.Lists.Users;

namespace com.LoanTek.API.Leads.Clients.Controllers
{
    /// <summary>
    /// Legacy web service for handling posts of XML and Query String posts of Lead data.
    /// </summary>
    //[RoutePrefix(domain url root +"/Legacy/{versionId}/Legacy/{encryptedClientId}")] '(domain url root +"/Deposit/{versionId}' is defined as a path on the server, so it is not needed here
    [RoutePrefix("Legacy")]
    public class LegacyController : AApiController
    {
        //Static constructor is used to initialize any static data or to perform a particular action that needs to be preformed once only
        //static LegacyController()
        //{
        //}

        /// <summary>
        /// Add a new Lead for Bankrate. Data is in XML format. Schema: <see cref="MortgageLead"/> LoanTek.Schemas.MortgageLead.
        /// </summary>
        /// <remarks>
        /// The Bankrate Data context is used.
        /// </remarks>
        /// <param name="request">The HttpRequestMessage request. Content-Type header should text/xml or application/xml. Content should be in XML format.</param>
        /// <returns cref="Api">Api object containing details about this web service.</returns>
        [Route("AddNewLead/Bankrate/XML")]
        [HttpPost]
        public HttpResponseMessage AddBankrateXmlLead(HttpRequestMessage request)
        {
            this.ResponseFormatter = new XmlMediaTypeFormatter();

            const string endpoint = "AddNewLead/Bankrate/XML";
            const int bankrateClientId = 903;
            const int bankrateUserId = 2383;

            var encryptedClientId = this.Request.RequestUri.ParseQueryString()["c"];
            int clientId = !string.IsNullOrEmpty(encryptedClientId) ? NullSafe.NullSafeInteger(Encryption.DecryptString(encryptedClientId)) : 0;
            if (clientId != bankrateClientId)
                return this.CreateErrorResponse(HttpStatusCode.Unauthorized, "Client is not valid for this web service.", endpoint);

            bool checkForDuplicate = false;
            return this.processXmlLead(request, encryptedClientId, bankrateUserId, endpoint, checkForDuplicate);
        }

        /// <summary>
        /// Add a new Lead. Data is in XML format. Schema: <see cref="MortgageLead"/> LoanTek.Schemas.MortgageLead.
        /// </summary>
        /// <param name="request">The HttpRequestMessage request. Content-Type header should text/xml or application/xml. Content should be in XML format.</param>
        /// <returns cref="Api">Api object containing details about this web service.</returns>
        [Route("AddNewLead/XML")]
        [HttpPost]
        public HttpResponseMessage AddXmlLead(HttpRequestMessage request)
        {
            const string endpoint = "AddNewLead/XML";
            var encryptedClientId = this.Request.RequestUri.ParseQueryString()["c"];
            return this.processXmlLead(request, encryptedClientId, 0, endpoint, true);
        }

        private HttpResponseMessage processXmlLead(HttpRequestMessage request, string encryptedClientId, int defaultUserId, string endpoint, bool checkForDuplicate = true)
        {
            this.ResponseFormatter = new XmlMediaTypeFormatter();

            var startTime = DateTime.Now;
            
            if (string.IsNullOrEmpty(encryptedClientId))
                return this.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing encrypted client id.", endpoint);

            var clientId = NullSafe.NullSafeInteger(Encryption.DecryptString(encryptedClientId));            
            AClient client = Master.Lists.Clients.GetClientById(clientId);    
            if (!client?.Active ?? true) //if null or not active reject...
                return this.CreateErrorResponse(HttpStatusCode.Unauthorized, "Client is invalid.", endpoint);

            //http://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor
            //Error is thrown here, this is 'expected' beaviour. Just ignore...
            var serializer = new XmlSerializer(typeof(MortgageLead));
            var dataLead = (MortgageLead)serializer.Deserialize(request.Content.ReadAsStreamAsync().Result);

            ALeadsResponse response = new ALeadsResponse();
            response.ApiEndPoint = endpoint;
            response.ClientDefinedIdentifier = dataLead.SourceLeadId;

            Lead lead;

            bool doInsert = true;
            if (checkForDuplicate)
            {
                ActionResponse<Lead> actionResponse = this.duplicateCheck(clientId, dataLead);
                if (actionResponse.Success) //true = dupicate
                {
                    doInsert = false;
                    response.Status = StatusType.Complete;
                    response.Message = "Duplicate";
                    response.LoanTekDefinedIdentifier = actionResponse.DataObject.Id.ToString();
                }
            }
            if (doInsert && (lead = LegacyLead.ConvertXmlSchema(dataLead)) != null)
            {
                lead.ClientID = clientId;

                //reset UserId to 0 if the User's ClientId does not match this client
                if (lead.UserID > 0 && Users.GetUserById(lead.UserID.GetValueOrDefault())?.ClientId != clientId)
                    lead.UserID = 0;
                if (lead.UserID == 0)
                    lead.UserID = defaultUserId;

                ActionResponse<Lead> actionResponse = new Biz.Contacts.Leads.Objects.Leads(DataContextType.Database, DataConnections.DataContextBankrateWrite).PutWithResponse(lead);
                if (actionResponse.Success)
                {
                    response.Status = StatusType.Complete;
                    response.Message = Types.Processing.StatusType.Success.ToString();
                    response.LoanTekDefinedIdentifier = actionResponse.DataObject?.Id.ToString();
                }
                else
                {
                    response.Status = StatusType.Error;
                    response.Message = response.Message;
                }
            }

            response.ExecutionTimeInMillisec = (int)(DateTime.Now - startTime).TotalMilliseconds;
            return Request.CreateResponse(HttpStatusCode.OK, response, this.ResponseFormatter);
        }

        private ActionResponse<Lead> duplicateCheck(int clientId, MortgageLead dataLead)
        {
            Lead lead = new Lead();
            //these need to initialized for DuplicateCheck
            lead.ClientID = clientId;
            lead.UserID = dataLead.UserId.ToInt();
            lead.FirstName = dataLead.Borrower?.FirstName;
            lead.LastName = dataLead.Borrower?.LastName;
            lead.Email = dataLead.ContactInfo?.Email;
            return lead.DuplicateCheck(DataConnections.DataContextBankrateRead);
        }
    }
}
