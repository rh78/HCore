using System.IO;
using System.Text.RegularExpressions;

namespace HCore.Web.Providers.Impl
{
    internal class HtmlTemplateFileIncludesProviderImpl : IHtmlIncludesProvider
    {
        public bool Applies { get; }

        public string HeaderIncludes { get; }

        public string BodyIncludes { get; }

        public string HeaderCssIncludes { get; }

        public string HeaderJsIncludes { get; }

        public string BodyJsIncludes { get; }

        public HtmlTemplateFileIncludesProviderImpl(string htmlFilePath, IHtmlTemplateFileIncludesProviderCustomProcessor customProcessor = null)
        {
            HeaderIncludes = "";
            BodyIncludes = "";
            HeaderCssIncludes = "";
            HeaderJsIncludes = "";
            BodyJsIncludes = "";

            Applies = File.Exists(htmlFilePath);

            if (Applies)
            {             
                string html = File.ReadAllText(htmlFilePath);

                if (customProcessor != null)
                {
                    html = customProcessor.ProcessHtml(htmlFilePath, html);
                }

                // header part of the HTML <head></head>
                Match header = Regex.Match(
                    html, 
                    "<head(?:\\s[^>]*)?>(.*?)</head(?:\\s[^>]*)?>", 
                    RegexOptions.Singleline | RegexOptions.IgnoreCase
                );

                var (includes, css, js) = ExtractCssAndScripts(header);
                HeaderIncludes += includes;
                HeaderCssIncludes += css;
                HeaderJsIncludes += js;

                // header part of the HTML <body></body>
                Match body = Regex.Match(
                    html, 
                    "<body(?:\\s[^>]*)?>(.*?)</body(?:\\s[^>]*)?>", 
                    RegexOptions.Singleline | RegexOptions.IgnoreCase
                );

                (includes, _, js) = ExtractCssAndScripts(body);
                BodyIncludes += includes;
                BodyJsIncludes += js;
                
                // ignore all other parts outside
            }
        }
        
        private (string, string, string) ExtractCssAndScripts(Match htmlPart)
        {
            string includes = "";
            string js = "";
            string css = "";
            
            while (htmlPart != null && htmlPart.Success)
            {
                Match partsOfInterest = Regex.Match(
                    htmlPart.Groups[1].Value,
                    "(<link(?:\\s[^>]*)?>|<script(?:\\s[^>]*)?>.*?</script(?:\\s[^>]*)?>)\\s*",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase
                );

                while (partsOfInterest.Success)
                {
                    string part = partsOfInterest.Groups[1].Value;

                    includes += $"{part}\n";

                    // add to other parts of the includes, too
                    if (
                        Regex.IsMatch(
                            part, 
                            "<link\\s(?:[^>]*\\s)?rel\\s*=\\s*[\"']?stylesheet[\"']?(?:\\s[^>]*)?>", 
                            RegexOptions.Singleline | RegexOptions.IgnoreCase
                        )
                    )
                    {
                        css += $"{part}\n";
                    }
                    else if (
                        Regex.IsMatch(
                            part, 
                            "<script", 
                            RegexOptions.Singleline | RegexOptions.IgnoreCase
                        )
                    )
                    {
                        js += $"{part}\n";
                    }

                    partsOfInterest = partsOfInterest.NextMatch();
                }

                htmlPart = htmlPart.NextMatch();
            }
            
            return (includes, css, js);
        }
    }
}
