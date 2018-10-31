using Microsoft.AspNetCore.Http;
using System;

namespace HCore.Web.Providers.Impl
{
    internal class UrlProviderImpl : IUrlProvider
    {
        public string BaseUrl { get; private set; }

        public UrlProviderImpl(IHttpContextAccessor httpContextAccessor)
        {
            var request = httpContextAccessor.HttpContext?.Request;

            if (request != null)
                BaseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/";
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
