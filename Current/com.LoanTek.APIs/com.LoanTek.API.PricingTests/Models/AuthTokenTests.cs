using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace com.LoanTek.API.PricingTests.Models
{
    [TestClass()]
    public class AuthTokenTests
    {
        int clientId = 5;
        string apiName = "PricingPricingEngineV2Controller.GetQuotes";
       
        [TestMethod()]
        public void AuthTokenTest()
        {
            string encryptedS = AuthToken.EncryptAuthKey(clientId, apiName);
            AuthToken obj = new AuthToken(encryptedS);
            Assert.AreEqual(clientId, obj.ClientId);
            Assert.AreEqual(apiName, obj.ApiName);
            
            obj = new AuthToken("");
            Assert.AreEqual(0, obj.ClientId); //this should be 0 because it was never set since the string was null or empty
            Assert.IsNull(obj.ApiName);
        }

        [TestMethod()]
        public void ParseAuthKeyTest()
        {
            string s = "399|BoB";
            var obj = AuthToken.ParseAuthKey(s);
            Assert.AreEqual(399, obj.ClientId);
            Assert.AreEqual("BoB", obj.ApiName);

            s = "BoB|399";
            obj = AuthToken.ParseAuthKey(s);
            Assert.AreNotEqual(399, obj.ClientId);
            Assert.AreNotEqual("BoB", obj.ApiName);
            Assert.AreEqual(-1, obj.ClientId); //this should be -1 because the string 'BoB' parsed as an int
            Assert.AreEqual("399", obj.ApiName);
        }

        [TestMethod()]
        public void EncryptAuthKeyTest()
        {
            string encryptedS = AuthToken.EncryptAuthKey(clientId, apiName);
            string decryptedS = AuthToken.DecryptAuthKey(encryptedS);
            Assert.AreEqual(clientId.ToString() + AuthToken.Delimiter.ToString() + apiName, decryptedS);
        }

        [TestMethod()]
        public void DecryptAuthKeyTest()
        {
            string encryptedS = AuthToken.EncryptAuthKey(399, "WebService/MyController/Method");
            string decryptedS = AuthToken.DecryptAuthKey(encryptedS);
            Assert.AreEqual("399"+ AuthToken.Delimiter +"WebService/MyController/Method", decryptedS);
        }
    }
}
