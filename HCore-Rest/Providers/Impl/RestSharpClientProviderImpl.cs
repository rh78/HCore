using HCore.Rest.Client;
using HCore.Rest.Client.Impl;
using System;

namespace HCore.Rest.Providers.Impl
{
    internal class RestSharpClientProviderImpl : IRestSharpClientProvider
    {
        private IRestSharpClient _restSharpClient;

        public IRestSharpClient GetRestSharpClient(string baseUrl)
        {
            if (_restSharpClient == null)
                _restSharpClient = new RestSharpClientImpl();

            _restSharpClient.BaseUrl = new Uri(baseUrl);

            return _restSharpClient;
        }
    }
}
