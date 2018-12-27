using System;
using Microsoft.Extensions.Configuration;

namespace HCore.Web.Providers.Impl
{
    internal class NonHttpContextUrlProviderImpl : INonHttpContextUrlProvider
    {
        public string BaseUrl { get; private set; }

        public NonHttpContextUrlProviderImpl(IConfiguration configuration)
        {
           string baseUrl = configuration["WebServer:BaseUrl"];

            if (!string.IsNullOrEmpty(baseUrl))
                BaseUrl = baseUrl;
            else
                BaseUrl = null;
        }

        public string BuildUrl(string path)
        {
            if (string.IsNullOrEmpty(BaseUrl))
                throw new Exception("No base url is available");

            return BaseUrl + path;
        }        
    }
}
