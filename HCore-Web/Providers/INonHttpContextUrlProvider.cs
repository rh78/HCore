using System;

namespace HCore.Web.Providers
{
    public interface INonHttpContextUrlProvider
    {
        Uri BaseUrl { get; }
        
        string BuildUrl(string path);        
    }
}
