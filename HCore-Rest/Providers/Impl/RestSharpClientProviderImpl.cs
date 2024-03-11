using HCore.Rest.Client;
using HCore.Rest.Client.Impl;
using Newtonsoft.Json;
using RestSharp;

namespace HCore.Rest.Providers.Impl
{
    internal class RestSharpClientProviderImpl : IRestSharpClientProvider
    {
        public IRestSharpClient GetRestSharpClient(string baseUrl)
        {
            var restClientOptions = new RestClientOptions(baseUrl);

            var restSharpClient = GetRestSharpClient(restClientOptions);

            return restSharpClient;
        }

        public IRestSharpClient GetRestSharpClient(RestClientOptions restClientOptions, JsonSerializerSettings jsonSerializerSettings = null)
        {
            var restSharpClient = new RestSharpClientImpl(restClientOptions, jsonSerializerSettings);

            return restSharpClient;
        }
    }
}
