using System;
using System.Threading.Tasks;
using RestSharp;

namespace ReinhardHolzner.HCore.RestSharp
{
    public interface IRestSharpClient
    {
        Uri BaseUrl { get; set; }

        Task<IRestResponse<TResponse>> ExecuteTaskAsync<TResponse>(RestRequest request);
    }
}
