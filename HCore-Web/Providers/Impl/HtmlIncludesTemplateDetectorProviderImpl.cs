using System;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace HCore.Web.Providers.Impl
{
    internal class HtmlIncludesTemplateDetectorProviderImpl : IHtmlIncludesDetectorProvider
    {
        private readonly IConfiguration _configuration;

        private readonly IWebHostEnvironment _hostEnvironment;

        private readonly IHtmlIncludesProvider _defaultIncludeProvider;

        public HtmlIncludesTemplateDetectorProviderImpl(
            IConfiguration configuration,
            IWebHostEnvironment hostingEnvironment)
        {
            _hostEnvironment = hostingEnvironment;
            _configuration = configuration;
            _defaultIncludeProvider = new SpaManifestJsonProviderImpl(configuration, hostingEnvironment);
        }

        public IHtmlIncludesProvider HtmlIncludesProviderForRequest(HttpRequest request)
        {
            string htmlFile;
            string htmlFileLowerCase;

            // check parameter
            if (request == null || !request.Path.HasValue)
            {
                return _defaultIncludeProvider;
            }

            // use default "index.html" for directories
            string pagePath = request.Path.Value;
            if (string.Equals("/", pagePath) || pagePath.EndsWith("/"))
            {
                pagePath += "index.html";
            }

            if (!pagePath.EndsWith(".html"))
            {
                pagePath += ".html";
            }

            string contentRootPath = _hostEnvironment.ContentRootPath;

            string clientAppPath = _configuration.GetValue<String>("Spa:RootPath");

            if (!string.IsNullOrEmpty(clientAppPath))
            {
                htmlFile = clientAppPath.StartsWith("file://") ?
                    $"{clientAppPath}/{pagePath}".Substring(7) : $"{contentRootPath}/{clientAppPath}/{pagePath}";

                htmlFileLowerCase = clientAppPath.StartsWith("file://") ?
                    $"{clientAppPath}/{pagePath.ToLower()}".Substring(7) :
                    $"{contentRootPath}/{clientAppPath}/{pagePath.ToLower()}";
            }
            else
            {
                htmlFile = $"{contentRootPath}/{pagePath}";

                htmlFileLowerCase = $"{contentRootPath}/{pagePath.ToLower()}";
            }

            // try with unchanged file name
            if (File.Exists(htmlFile))
            {
                return new HtmlTemplateFileIncludesProviderImpl(htmlFile);
            }

            // try with a lowercased file name
            return File.Exists(htmlFileLowerCase) ?
                new HtmlTemplateFileIncludesProviderImpl(htmlFileLowerCase) : _defaultIncludeProvider;
        }
    }
}
