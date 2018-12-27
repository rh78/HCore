using Microsoft.AspNetCore.Http;
using System;
using Microsoft.Extensions.Configuration;

namespace HCore.Web.Providers.Impl
{
    internal class NonHttpContextUrlProviderImpl : INonHttpContextUrlProvider
    {
        public Uri BaseUrl { get; private set; }

        public NonHttpContextUrlProviderImpl(IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentException("Configuration is needed for UrlProvider.");

            var baseUrl = configuration.GetValue<string>("Identity:DefaultClient:Audience");

            if (baseUrl == null)
                throw new Exception("Identity:DefaultClient:Audience is missing.");

            BaseUrl = new Uri(baseUrl);
        }

        public string BuildUrl(string path)
        {
            return new Uri(BaseUrl, path).AbsoluteUri;
        }        
    }
}
