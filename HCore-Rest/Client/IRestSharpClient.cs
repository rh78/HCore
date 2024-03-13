using System;
using System.Threading.Tasks;
using RestSharp;

namespace HCore.Rest.Client
{
    public interface IRestSharpClient
    {
        Uri BaseUrl { get; }

        Task<RestResponse<TResponse>> ExecuteTaskAsync<TResponse>(RestRequest request);

        RestClient Client { get; }

        string GetLogContent(RestRequest request, RestResponse response);
    }
}
