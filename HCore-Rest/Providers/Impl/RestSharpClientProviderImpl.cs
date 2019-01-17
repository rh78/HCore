using HCore.Rest.Client;
using HCore.Rest.Client.Impl;
using System;

namespace HCore.Rest.Providers.Impl
{
    internal class RestSharpClientProviderImpl : IRestSharpClientProvider
    {
        public IRestSharpClient GetRestSharpClient(string baseUrl)
        {
            var restSharpClient = new RestSharpClientImpl();

            restSharpClient.BaseUrl = new Uri(baseUrl);

            return restSharpClient;
        }
    }
}
