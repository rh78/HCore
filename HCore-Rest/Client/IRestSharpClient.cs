using System;
using System.Threading.Tasks;
using RestSharp;

namespace HCore.Rest.Client
{
    public interface IRestSharpClient
    {
        Uri BaseUrl { get; set; }

        Task<IRestResponse<TResponse>> ExecuteTaskAsync<TResponse>(RestRequest request);

        RestClient Client { get; }

        string GetLogContent(IRestRequest request, IRestResponse response);
    }
}
