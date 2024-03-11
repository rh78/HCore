using HCore.Rest.Client;
using Newtonsoft.Json;
using RestSharp;

namespace HCore.Rest.Providers
{
    public interface IRestSharpClientProvider
    {
        IRestSharpClient GetRestSharpClient(string baseUrl);

        IRestSharpClient GetRestSharpClient(RestClientOptions restClientOptions, JsonSerializerSettings jsonSerializerSettings = null);
    }
}
