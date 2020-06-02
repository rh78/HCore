using System;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace HCore.Web.Providers.Impl
{
    internal class SpaManifestJsonProviderImpl : IHtmlIncludesProvider
    {
        public bool Applies { get; }

        public string HeaderIncludes { get; }

        public string BodyIncludes { get; }

        public string HeaderCssIncludes { get; }

        public string HeaderJsIncludes { get; }
        public string BodyJsIncludes { get; }

        public SpaManifestJsonProviderImpl(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            HeaderIncludes = "";
            BodyIncludes = "";
            HeaderCssIncludes = "";
            HeaderJsIncludes = "";
            BodyJsIncludes = "";

            string appRootPath = configuration.GetValue<String>("Spa:RootPath");

            string contentRootPath = hostingEnvironment.ContentRootPath;

            // 0.js is a development build artifact

            Applies = File.Exists($"{contentRootPath}/{appRootPath}/manifest.json") &&
                      !File.Exists($"{contentRootPath}/{appRootPath}/0.js");
                
            if (Applies)
            {             
                string json = File.ReadAllText($"{contentRootPath}/{appRootPath}/manifest.json");

                var mappings = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                foreach (var key in mappings.Keys)
                {
                    string value = mappings[key];

                    if (key.StartsWith("js/chunk"))
                    {
                        string jsChunk = value;

                        HeaderIncludes += $"<link href={jsChunk} rel=prefetch>\n";
                        HeaderJsIncludes += $"<link href={jsChunk} rel=prefetch>\n";
                    }
                }

                string appCss = mappings["app.css"];

                HeaderIncludes += $"<link href={appCss} rel=preload as=style>\n";
                HeaderCssIncludes += $"<link href={appCss} rel=preload as=style>\n";

                string chunkVendorsCss = mappings["chunk-vendors.css"];

                HeaderIncludes += $"<link href={chunkVendorsCss} rel=preload as=style>\n";
                HeaderCssIncludes += $"<link href={chunkVendorsCss} rel=preload as=style>\n";

                string appJs = mappings["app.js"];

                HeaderIncludes += $"<link href={appJs} rel=preload as=script>\n";
                HeaderJsIncludes += $"<link href={appJs} rel=preload as=script>\n";

                string chunkVendorsJs = mappings["chunk-vendors.js"];

                HeaderIncludes += $"<link href={chunkVendorsJs} rel=preload as=script>\n";
                HeaderJsIncludes += $"<link href={chunkVendorsJs} rel=preload as=script>\n";

                BodyIncludes += $"<script src={chunkVendorsJs}></script>\n";
                BodyJsIncludes += $"<script src={chunkVendorsJs}></script>\n";

                HeaderIncludes += $"<link href={chunkVendorsCss} rel=stylesheet>\n";
                HeaderCssIncludes += $"<link href={chunkVendorsCss} rel=stylesheet>\n";

                HeaderIncludes += $"<link href={appCss} rel=stylesheet>\n";
                HeaderCssIncludes += $"<link href={appCss} rel=stylesheet>\n";

                BodyIncludes += $"<script src={appJs}></script>\n";
                BodyJsIncludes += $"<script src={appJs}></script>\n";
            }
        }
    }
}
