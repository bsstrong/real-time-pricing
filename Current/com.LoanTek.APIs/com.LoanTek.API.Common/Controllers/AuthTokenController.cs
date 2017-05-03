using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;

namespace com.LoanTek.API.Common.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [EnableCors(origins: "http://localhost:51259", headers: "*", methods: "*")]
    //[IPHostValidation]
    [RoutePrefix("AuthToken")]
    public class AuthTokenController : ApiController
    {
        [HttpGet]
        [Route("Encrypt/1.0")]
        //[EnableCors(origins: "*", headers: "*", methods: "*")]
        public string Encrypt(int clientId, string apiName)
        {
            //Debug.WriteLine("clientId:" + clientId);
            //Debug.WriteLine("apiName:" + apiName);
            return AuthToken.EncryptAuthKey(clientId, apiName);
        }

        [HttpGet]
        [Route("Decrypt/1.0")]
        public string Decrypt(string encryptedAuthToken)
        {
            return AuthToken.DecryptAuthKey(encryptedAuthToken);
        }

    }
}
