using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace HCore.Rest.Client.Impl
{
    internal class RestSharpClientImpl : IRestSharpClient
    {
        private static readonly JsonSerializerSettings _defaultJsonSerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
        };

        public RestClient Client { get; private set; }

        private const int RequestRetryCount = 5;
        private const int BackoffRate = 100; // ms

        public RestSharpClientImpl(RestClientOptions restClientOptions, JsonSerializerSettings jsonSerializerSettings = null)
        {
            ArgumentNullException.ThrowIfNull(restClientOptions);

            jsonSerializerSettings ??= _defaultJsonSerializerSettings;

            Client = new RestClient(restClientOptions, configureSerialization: sc => sc.UseNewtonsoftJson(jsonSerializerSettings));
        }

        public RestSharpClientImpl(RestClientOptions restClientOptions, ConfigureSerialization configureSerialization)
        {
            ArgumentNullException.ThrowIfNull(restClientOptions);
            ArgumentNullException.ThrowIfNull(configureSerialization);

            Client = new RestClient(restClientOptions, configureSerialization: configureSerialization);
        }

        public Uri BaseUrl { get => Client.Options.BaseUrl; }

        public async Task<RestResponse<TResponse>> ExecuteTaskAsync<TResponse>(RestRequest request)
        {
            return await ExecuteTaskExponentialBackoffAsync<TResponse>(request).ConfigureAwait(false);
        }

        private async Task<RestResponse<TResponse>> ExecuteTaskExponentialBackoffAsync<TResponse>(RestRequest request)
        {
            RestResponse<TResponse> response = null;

            int count = 0;

            bool failedWithBackoff = false;

            do
            {
                response = await Client.ExecuteAsync<TResponse>(request).ConfigureAwait(false);

                failedWithBackoff = false;

                switch (response.ResponseStatus)
                {
                    case ResponseStatus.Aborted:
                        failedWithBackoff = true;

                        break;
                    case ResponseStatus.TimedOut:
                        failedWithBackoff = true;

                        break;
                    default:
                        // ok

                        break;
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    failedWithBackoff = true;

                if (failedWithBackoff)
                {
                    count++;

                    if (count <= RequestRetryCount)
                        await Task.Delay((count ^ 2) * BackoffRate).ConfigureAwait(false);
                    else
                        break;
                }
            } while (failedWithBackoff);

            return response;
        }

        public string GetLogContent(RestRequest request, RestResponse response)
        {
            var requestToLog = new
            {
                resource = request.Resource,

                // Parameters are custom anonymous objects in order to have the parameter type as a nice string
                // otherwise it will just show the enum value
                parameters = request.Parameters.Select(parameter => new
                {
                    name = parameter.Name,
                    value = parameter.Value,
                    type = parameter.Type.ToString()
                }),

                // ToString() here to have the method as a nice string otherwise it will just show the enum value
                method = request.Method.ToString(),

                // This will generate the actual Uri used in the request
                uri = Client.BuildUri(request),
            };

            var responseToLog = new
            {
                statusCode = response.StatusCode,
                content = response.Content,
                headers = response.Headers,

                // The Uri that actually responded (could be different from the requestUri if a redirection occurred)

                responseUri = response.ResponseUri,
                errorMessage = response.ErrorMessage,
            };

            return $"Request: {JsonConvert.SerializeObject(requestToLog)}, Response: {JsonConvert.SerializeObject(responseToLog)}";
        }
    }
}
