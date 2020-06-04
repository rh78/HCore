using Microsoft.AspNetCore.Http;

namespace HCore.Web.Providers
{
    public interface IHtmlIncludesDetectorProvider
    {
        /// <summary>
        /// Creates a <see cref="IHtmlIncludesProvider"/> based on the the current requested page.
        /// <remarks>Based on the page URI path, query or host name, the HTML includes may be different.
        /// So, the detector implements mechanisms to decide what to include based on the requested page.</remarks>
        /// </summary>
        /// <param name="context">The current request context, that can be used to determine proper HTML includes for
        /// the requested page.</param>
        /// <returns></returns>
        IHtmlIncludesProvider HtmlIncludesProviderForRequest(HttpContext context);
    }
}
