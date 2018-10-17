using System;
using System.Net;
using System.Threading.Tasks;
using RestSharp;

namespace HCore.Rest.Impl
{
    internal class RestSharpClientImpl : IRestSharpClient
    {
        private readonly RestClient _restClient;

        private const int RequestRetryCount = 5;
        private const int BackoffRate = 100; // ms

        public RestSharpClientImpl()
        {
            _restClient = new RestClient();
        }

        public Uri BaseUrl { get => _restClient.BaseUrl; set => _restClient.BaseUrl = value; }

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
                response = await _restClient.ExecuteTaskAsync<TResponse>(request).ConfigureAwait(false);

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
    }
}
