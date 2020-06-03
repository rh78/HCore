using System;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace HCore.Web.Providers.Impl
{
    internal class HtmlIncludesTemplateDetectorProviderImpl : IHtmlIncludesDetectorProvider
    {
        private readonly IConfiguration _configuration;

        private readonly IWebHostEnvironment _hostEnvironment;

        private readonly IMemoryCache _memoryCache;

        private readonly IHtmlIncludesProvider _defaultIncludeProvider;

        public HtmlIncludesTemplateDetectorProviderImpl(
            IConfiguration configuration,
            IWebHostEnvironment hostingEnvironment,
            IMemoryCache memoryCache)
        {
            _hostEnvironment = hostingEnvironment;
            _configuration = configuration;
            _memoryCache = memoryCache;
 
            // default provider will set its "Applies" property to "False", as the file is "null"
            _defaultIncludeProvider = new HtmlTemplateFileIncludesProviderImpl(null);
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

            // try unchanged file name or lower case file name. Linux is case sensitive. With WebPack project,
            // lower case file names are created. So, if the original case file can not be found, try with lower case
            // version. But do not perform real any-case detection.
            if (!File.Exists(htmlFile))
            {
                _memoryCache?.Remove(htmlFile);
                htmlFile = htmlFileLowerCase;
            }

            // parse file
            if (File.Exists(htmlFile))
            {
                DateTime fileTime = File.GetLastWriteTime(htmlFile);

                IHtmlIncludesProvider htmlTemplate = null;

                // lookup in the cache
                if (
                    _memoryCache != null
                    && _memoryCache.TryGetValue(htmlFile, out Tuple<IHtmlIncludesProvider, DateTime> cacheItem)
                    && cacheItem != null && DateTime.Compare(cacheItem.Item2, fileTime) >= 0
                )
                {
                    htmlTemplate = cacheItem.Item1;
                }

                // parse file
                if (htmlTemplate == null)
                {
                    htmlTemplate = new HtmlTemplateFileIncludesProviderImpl(htmlFile);
                }

                // store the newly created value or update current value
                if (_memoryCache != null)
                {
                    _memoryCache.Remove(htmlFile);

                    var entry = _memoryCache.CreateEntry(htmlFile);
                    entry.Value = new Tuple<IHtmlIncludesProvider, DateTime>(htmlTemplate, DateTime.Now);
                }
            }
            else
            {
                _memoryCache?.Remove(htmlFile);
            }

            return _defaultIncludeProvider;
        }
    }
}
