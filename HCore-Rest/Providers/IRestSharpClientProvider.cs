using HCore.Rest.Client;
using Newtonsoft.Json;
using RestSharp;

namespace HCore.Rest.Providers
{
    public interface IRestSharpClientProvider
    {
        IRestSharpClient GetRestSharpClient(string baseUrl, JsonSerializerSettings jsonSerializerSettings = null);

        IRestSharpClient GetRestSharpClient(RestClientOptions restClientOptions, JsonSerializerSettings jsonSerializerSettings = null);

        IRestSharpClient GetRestSharpClientXml(string baseUrl);

        IRestSharpClient GetRestSharpClientXml(RestClientOptions restClientOptions);
    }
}
