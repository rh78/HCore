using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HCore.Rest.Client.Impl
{
    public abstract class OpenApiClientImpl
    {
        public string BaseUrl { get; set;  }
        public string AccessToken { get; set; }        

        public OpenApiClientImpl()
        {
            
        }

#pragma warning disable CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        protected virtual async Task<HttpRequestMessage> CreateHttpRequestMessageAsync(CancellationToken cancellationToken)
#pragma warning restore CS1998 // Bei der asynchronen Methode fehlen "await"-Operatoren. Die Methode wird synchron ausgeführt.
        {
            if (string.IsNullOrEmpty(BaseUrl))
                throw new Exception("Please set the base URL before using the API client");

            var httpRequestMessage = new HttpRequestMessage();

            if (string.IsNullOrEmpty(AccessToken))
                throw new Exception("Please set the access token before using the API client");

            httpRequestMessage.Headers.TryAddWithoutValidation("Authorization", $"Bearer {AccessToken}");

            return httpRequestMessage;
        }
    }
}