using System;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.Http
{
    public static class HttpContextExtensions
    {
        private const string _scriptNonceKey = "ScriptNonce";

        public static string GetScriptNonce(this HttpContext context)
        {
            if (!context.Items.TryGetValue(_scriptNonceKey, out object scriptNonce))
            {
                using (var randomNumberGenerator = RandomNumberGenerator.Create())
                {
                    var nonceBytes = new byte[32];

                    randomNumberGenerator.GetBytes(nonceBytes);

                    scriptNonce = Convert.ToBase64String(nonceBytes);
                }

                context.Items[_scriptNonceKey] = scriptNonce;
            }

            return (string)scriptNonce;
        }
    }
}
