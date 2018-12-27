using System;

namespace HCore.Web.Providers
{
    public interface INonHttpContextUrlProvider
    {
        string BaseUrl { get; }
        
        string BuildUrl(string path);        
    }
}
