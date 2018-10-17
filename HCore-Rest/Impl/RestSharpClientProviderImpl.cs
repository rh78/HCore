namespace HCore.Rest.Impl
{
    internal class RestSharpClientProviderImpl : IRestSharpClientProvider
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
