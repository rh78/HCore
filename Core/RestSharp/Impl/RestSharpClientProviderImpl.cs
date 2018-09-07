using ReinhardHolzner.HCore.RestSharp;
using ReinhardHolzner.HCore.RestSharp.Impl;

namespace ReinhardHolzner.Core.RestSharp.Impl
{
    public class RestSharpClientProviderImpl : IRestSharpClientProvider
    {
        private IRestSharpClient _restSharpClient;

        public IRestSharpClient GetRestSharpClient()
        {
            if (_restSharpClient == null)
                _restSharpClient = new RestSharpClientImpl();

            return _restSharpClient;
        }
    }
}
