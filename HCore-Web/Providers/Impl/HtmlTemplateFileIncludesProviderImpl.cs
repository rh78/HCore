using System.IO;
using System.Text.RegularExpressions;

namespace HCore.Web.Providers.Impl
{
    public class HtmlTemplateFileIncludesProviderImpl : IHtmlIncludesProvider
    {
        public bool Applies { get; }

        private string _headerIncludes;

        private string _bodyIncludes;

        public HtmlTemplateFileIncludesProviderImpl(string htmlFilePath, IHtmlTemplateFileIncludesProviderCustomProcessor customProcessor = null)
        {
            _headerIncludes = "";
            _bodyIncludes = "";

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

                var includes = ExtractCssAndScripts(header);
                _headerIncludes += includes;

                // header part of the HTML <body></body>
                Match body = Regex.Match(
                    html, 
                    "<body(?:\\s[^>]*)?>(.*?)</body(?:\\s[^>]*)?>", 
                    RegexOptions.Singleline | RegexOptions.IgnoreCase
                );

                includes = ExtractCssAndScripts(body);
                _bodyIncludes += includes;
                
                // ignore all other parts outside
            }
        }
        
        private string ExtractCssAndScripts(Match htmlPart)
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
                        part = part.Replace("<script", "<script nonce=\"NONCE-PLACEHOLDER\"");

                        js += $"{part}\n";
                    }
                    else if (part.StartsWith("<link") && part.EndsWith("as=\"script\">"))
                    {
                        part = part.Replace("<link", "<link nonce=\"NONCE-PLACEHOLDER\"");
                    }

                    includes += $"{part}\n";

                    partsOfInterest = partsOfInterest.NextMatch();
                }

                htmlPart = htmlPart.NextMatch();
            }
            
            return includes;
        }

        public string GetHeaderIncludes(string scriptNonce)
        {
            return _headerIncludes?.Replace("NONCE-PLACEHOLDER", scriptNonce);
        }

        public string GetBodyIncludes(string scriptNonce)
        {
            return _bodyIncludes?.Replace("NONCE-PLACEHOLDER", scriptNonce);
        }
    }
}
