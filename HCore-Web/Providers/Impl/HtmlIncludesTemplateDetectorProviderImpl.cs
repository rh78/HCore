using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace HCore.Web.Providers.Impl
{
    internal class HtmlIncludesTemplateDetectorProviderImpl : IHtmlIncludesDetectorProvider
    {
        private readonly IConfiguration _configuration;

        private readonly IWebHostEnvironment _hostEnvironment;

        private readonly IHtmlIncludesProvider _defaultIncludeProvider;

        private readonly IHtmlTemplateFileIncludesProviderCustomProcessor _htmlTemplateFileIncludesProviderCustomProcessor;

        private readonly Dictionary<string, IHtmlIncludesProvider> _htmlIncludeProviders =
            new Dictionary<string, IHtmlIncludesProvider>();

        public HtmlIncludesTemplateDetectorProviderImpl(
            IConfiguration configuration,
            IWebHostEnvironment hostingEnvironment,
            IServiceProvider serviceProvider)
        {
            _hostEnvironment = hostingEnvironment;
            _configuration = configuration;

            _htmlTemplateFileIncludesProviderCustomProcessor = serviceProvider.GetService<IHtmlTemplateFileIncludesProviderCustomProcessor>();

            // default provider will set its "Applies" property to "False", as the file is "null"
            _defaultIncludeProvider = new HtmlTemplateFileIncludesProviderImpl(null, _htmlTemplateFileIncludesProviderCustomProcessor);

            ParseAllHtmlFiles();
        }

        public IHtmlIncludesProvider HtmlIncludesProviderForRequest(HttpContext context)
        {
            // check parameter
            if (context == null)
            {
                return _defaultIncludeProvider;
            }

            // try URL path at first
            var htmlTemplateProvider = GetHtmlIncludesProviderForPath(context.Request?.Path.Value);
            if (htmlTemplateProvider != null)
            {
                return htmlTemplateProvider;
            }

            // try with endpoint path
            htmlTemplateProvider = GetHtmlIncludesProviderForPath($"{context.GetEndpoint().DisplayName}");
            return htmlTemplateProvider ?? _defaultIncludeProvider;
        }

        public IHtmlIncludesProvider GetHtmlIncludesProviderForPath(string path)
        {
            // check parameter
            if (path == null)
            {
                return null;
            }

            // use default "index.html" for directories
            string pagePath = string.IsNullOrEmpty(path) ? "/" : path;
            if (string.Equals("/", pagePath) || pagePath.EndsWith("/"))
            {
                pagePath += "index.html";
            }

            // enforce .html file to be used
            if (!pagePath.EndsWith(".html"))
            {
                pagePath += ".html";
            }

            // use case insensitive matching, thus use lower case file names only
            return _htmlIncludeProviders.TryGetValue(pagePath.ToLower(), out IHtmlIncludesProvider provider)
                ? provider
                : null;
        }

        private void ParseAllHtmlFiles()
        {
            foreach (var fileData in GetHtmlFilesInRootDirectory())
            {
                var filePath = fileData.Item1;
                var fullPath = fileData.Item2;

                filePath = filePath.Replace("\\", "/").ToLower();

                _htmlIncludeProviders.Add(filePath, new HtmlTemplateFileIncludesProviderImpl(fullPath, _htmlTemplateFileIncludesProviderCustomProcessor));

                if (filePath.StartsWith("/ecb") &&
                    !string.Equals(filePath, "/ecb/index.html"))
                {
                    // map ECB to root for account UI

                    _htmlIncludeProviders.Add(filePath.Replace("/ecb", ""), new HtmlTemplateFileIncludesProviderImpl(fullPath, _htmlTemplateFileIncludesProviderCustomProcessor));
                }
            }
        }

        private List<(string, string)> GetHtmlFilesInRootDirectory()
        {
            List<(string, string)> allHtmlFiles = new List<(string, string)>();

            var baseDir = GetRootPath();
            foreach (string file in Directory.EnumerateFiles(baseDir, "*.html", SearchOption.AllDirectories))
            {
                // do something
                allHtmlFiles.Add((file.Substring(baseDir.Length), file));
            }

            return allHtmlFiles;
        }

        private string GetRootPath()
        {
            string contentRootPath = _hostEnvironment.ContentRootPath;

            string clientAppPath = _configuration.GetValue<string>("Spa:RootPath");

            if (!string.IsNullOrEmpty(clientAppPath))
            {
                return clientAppPath.StartsWith("file://") ?
                    clientAppPath.Substring(7) : $"{contentRootPath}/{clientAppPath}";
            }
            else
            {
                return contentRootPath;
            }
        }
    }
}
