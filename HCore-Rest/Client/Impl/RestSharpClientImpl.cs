using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HCore.Rest.Client.Impl.Serializer;
using Newtonsoft.Json;
using RestSharp;

namespace HCore.Rest.Client.Impl
{
    // TODO: upgrade to newest RestSharp client (breaking)
    // see https://restsharp.dev/v107/#restsharp-v107

    internal class RestSharpClientImpl : IRestSharpClient
    {
        public RestClient Client { get; private set; }

        private const int RequestRetryCount = 5;
        private const int BackoffRate = 100; // ms

        public RestSharpClientImpl()
        {
            Client = new RestClient();

            Client.AddHandler("application/json", () => { return NewtonsoftJsonSerializer.Default; });
            Client.AddHandler("text/json", () => { return NewtonsoftJsonSerializer.Default; });
            Client.AddHandler("text/x-json", () => { return NewtonsoftJsonSerializer.Default; });
            Client.AddHandler("text/javascript", () => { return NewtonsoftJsonSerializer.Default; });
            Client.AddHandler("*+json", () => { return NewtonsoftJsonSerializer.Default; });
        }

        public Uri BaseUrl { get => Client.BaseUrl; set => Client.BaseUrl = value; }

        public async Task<IRestResponse<TResponse>> ExecuteTaskAsync<TResponse>(RestRequest request)
        {
            return await ExecuteTaskExponentialBackoffAsync<TResponse>(request).ConfigureAwait(false);
        }

        private async Task<IRestResponse<TResponse>> ExecuteTaskExponentialBackoffAsync<TResponse>(RestRequest request)
        {
            IRestResponse<TResponse> response = null;

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

        public string GetLogContent(IRestRequest request, IRestResponse response)
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
