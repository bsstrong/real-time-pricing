using System;

namespace com.LoanTek.API
{
    public class AuthToken
    {
        //public const string AdminByPassToken = "";
        public const char Delimiter = '|';
        private const string delimiterS = "|";

        public AuthToken(){}
        public AuthToken(string encryptedAuthToken)
        {
            if (string.IsNullOrEmpty(encryptedAuthToken))
                return;
            string decryptAuthKey = DecryptAuthKey(encryptedAuthToken);
            var obj = ParseAuthKey(decryptAuthKey);
            this.ApiName = obj?.ApiName;
            this.ClientId = obj?.ClientId ?? 0;
        }

        public string ApiName { get; set; }
        public int ClientId { get; set; }
        
        public static AuthToken ParseAuthKey(string s)
        {
            AuthToken authToken = new AuthToken();
            if (!string.IsNullOrEmpty(s))
            {
                var data = s.Split(Delimiter);
                if (data.Length > 1)
                {
                    authToken.ClientId = NullSafe.NullSafeInteger(data[0]);
                    authToken.ApiName = data[1];
                }
            }
            return authToken;
        }

        public static string EncryptAuthKey(int clientId, string apiName)
        {
            try
            {
                return Encryption.EncodeToBase64URL(Encryption.EncryptString(clientId.ToString() + delimiterS + apiName));
            }
            catch (Exception) { return null; }
            
        }

        public static string DecryptAuthKey(string encryptedAuthToken)
        {
            try
            {
                encryptedAuthToken = Encryption.DecryptString(Encryption.DecodeFromBase64URL(encryptedAuthToken));
                if (encryptedAuthToken.EndsWith("/"))
                    encryptedAuthToken.Remove(encryptedAuthToken.Length - 1);
                return encryptedAuthToken;
            }
            catch (Exception) { return null; }
        }

        
    }
}