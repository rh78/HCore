using System;
using System.Threading.Tasks;
using RestSharp;

namespace ReinhardHolzner.Core.Rest
{
    public interface IRestSharpClient
    {
        Uri BaseUrl { get; set; }

        Task<IRestResponse<TResponse>> ExecuteTaskAsync<TResponse>(RestRequest request);
    }
}
