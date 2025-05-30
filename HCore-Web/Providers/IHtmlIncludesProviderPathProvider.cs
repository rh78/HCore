using Microsoft.AspNetCore.Http;

namespace HCore.Web.Providers
{
    public interface IHtmlIncludesProviderPathProvider
    {
        string GetHtmlIncludesProviderPath(HttpContext context);
    }
}
