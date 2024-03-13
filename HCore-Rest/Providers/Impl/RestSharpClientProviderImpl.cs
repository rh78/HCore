using HCore.Rest.Client;
using HCore.Rest.Client.Impl;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers.Xml;

namespace HCore.Rest.Providers.Impl
{
    internal class RestSharpClientProviderImpl : IRestSharpClientProvider
    {
        public IRestSharpClient GetRestSharpClient(string baseUrl, JsonSerializerSettings jsonSerializerSettings = null)
        {
            var restClientOptions = new RestClientOptions(baseUrl);

            var restSharpClient = GetRestSharpClient(restClientOptions, jsonSerializerSettings);

            return restSharpClient;
        }

        public IRestSharpClient GetRestSharpClient(RestClientOptions restClientOptions, JsonSerializerSettings jsonSerializerSettings = null)
        {
            var restSharpClient = new RestSharpClientImpl(restClientOptions, jsonSerializerSettings);

            return restSharpClient;
        }

        public IRestSharpClient GetRestSharpClientXml(string baseUrl)
        {
            var restClientOptions = new RestClientOptions(baseUrl);

            var restSharpClient = GetRestSharpClientXml(restClientOptions);

            return restSharpClient;
        }

        public IRestSharpClient GetRestSharpClientXml(RestClientOptions restClientOptions)
        {
            var restSharpClient = new RestSharpClientImpl(restClientOptions, configureSerialization => configureSerialization.UseXmlSerializer(useAttributeDeserializer: true));

            return restSharpClient;
        }
    }
}
