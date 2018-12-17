using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace HCore.Web.Providers.Impl
{
    internal class SpaManifestJsonProviderImpl : ISpaManifestJsonProvider
    {
        public bool Applies { get; }

        public string HeaderIncludes { get; }

        public string BodyIncludes { get; }

        public SpaManifestJsonProviderImpl(IHostingEnvironment hostingEnvironment)
        {
            HeaderIncludes = "";
            BodyIncludes = "";

            string contentRootPath = hostingEnvironment.ContentRootPath;

            // 0.js is development build artifact

            Applies = File.Exists($"{contentRootPath}/ClientApp/build/manifest.json") &&
                      !File.Exists($"{contentRootPath}/ClientApp/build/0.js");
                
            if (Applies)
            {             
                string json = File.ReadAllText($"{contentRootPath}/ClientApp/build/manifest.json");

                var mappings = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                foreach (var key in mappings.Keys)
                {
                    string value = mappings[key];

                    if (key.StartsWith("js/chunk"))
                    {
                        string jsChunk = value;

                        HeaderIncludes += $"<link href=/{jsChunk} rel=prefetch>\n";
                    }
                }

                string appCss = mappings["app.css"];

                HeaderIncludes += $"<link href=/{appCss} rel=preload as=style>\n";

                string chunkVendorsCss = mappings["chunk-vendors.css"];

                HeaderIncludes += $"<link href=/{chunkVendorsCss} rel=preload as=style>\n";

                string appJs = mappings["app.js"];

                HeaderIncludes += $"<link href=/{appJs} rel=preload as=script>\n";

                string chunkVendorsJs = mappings["chunk-vendors.js"];

                HeaderIncludes += $"<link href=/{chunkVendorsJs} rel=preload as=script>\n";
                BodyIncludes += $"<script src=/{chunkVendorsJs}></script>\n";

                HeaderIncludes += $"<link href=/{chunkVendorsCss} rel=stylesheet>\n";

                HeaderIncludes += $"<link href=/{appCss} rel=stylesheet>\n";

                BodyIncludes += $"<script src=/{appJs}></script>\n";
            }
        }
    }
}
