using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace HCore.Identity.Attributes
{
    /// <summary>
    /// Adds special HTTP header to a response.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The HTTP headers to set are taken from the configuration with key
    /// <code>WebServer:http:headers:default:security</code> (see: <see cref="SecurityHeadersBaseConfigKey"/>).
    /// </para>
    /// <para>
    /// It is possible to set a special <code>tagName</code> via constructor. Then additional customized settings
    /// are read from the configuration that will override the default settings. The default settings are stored
    /// with the tag name being set to <see cref="DefaultTagName"/>. So any tag name value different than that, will
    /// override the default settings. To make use of that, you can use the following to apply this attribute to your
    /// class:
    /// <example>
    /// [TypeFilter(typeof(SecurityHeadersAttribute), Arguments = new object[] {"custom-tag"})]
    /// public class PageWithSecurityHeaders : PageModel
    /// {
    /// }
    /// </example>
    ///
    /// </para>
    /// <para>
    /// If a configuration key hold a <code>null</code> value, then the HTTP header will not be sent to the client.
    /// The same applies to CSP header parts.
    /// </para>
    /// <para>
    /// Only missing headers are set to the response. If a header has been set to the response previously, then the
    /// configuration will be ignored for this response.
    /// </para>
    /// </remarks>
    public class SecurityHeadersAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// The tag name to store default values. It is appended to <see cref="HeadersBaseConfigKey"/>
        /// </summary>
        public static readonly string DefaultTagName = "default";

        /// <summary>
        /// The base configuration keys for all header settings.
        /// </summary>
        /// <remarks>
        /// Value: <code>HeadersBaseConfigKey = "WebServer:http:headers:"</code>
        /// </remarks>
        public static readonly string HeadersBaseConfigKey = "WebServer:http:headers:";

        /// <summary>
        /// The configuration section key to read all security header from that need to be set.
        /// </summary>
        /// <remarks>
        /// Value: <code>SecurityHeadersBaseConfigKey = $"{HeadersBaseConfigKey}{DefaultTagName}:security"</code>
        /// </remarks>
        public static readonly string SecurityHeadersBaseConfigKey = $"{HeadersBaseConfigKey}{DefaultTagName}:security";

        /// <summary>
        /// The configuration section key to read Content-Security-Policy header parts.
        /// </summary>
        /// <remarks>The CSP header consists of various parts. To make it easier to share common parts and just
        /// overwrite some other parts, these are split into separate configuration keys. The name of the part
        /// is the name of the key. https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP</remarks>
        public static readonly string CspHeaderBaseConfigKey = $"{HeadersBaseConfigKey}{DefaultTagName}:security-csp";

        private readonly IConfiguration _configuration;

        private readonly string _tagName;

        /// <summary>
        /// Creates are new header filter with configuration and custom tag name.
        /// </summary>
        /// <param name="configuration">The configuration, usually injected by the dependency syste</param>
        /// <param name="tagName">A custom tag name, which can be set upon applying this filter to a class. So the
        /// tag name is only specific to a class and not to a request.</param>
        public SecurityHeadersAttribute(IConfiguration configuration, string tagName = null)
        {
            this._configuration = configuration;
            _tagName = tagName;
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var result = context.Result;

            if (result is ViewResult || result is PageResult || result is LocalRedirectResult)
            {
                AddSecurityHeaders(context);

                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy
                var csp = GetConfiguredCspHeader();

                // once for standards compliant browsers
                if (
                    !string.IsNullOrWhiteSpace(csp)
                    && !context.HttpContext.Response.Headers.ContainsKey("Content-Security-Policy")
                )
                {
                    context.HttpContext.Response.Headers.Add("Content-Security-Policy".ToLower(), csp);
                }

                // IE just does trouble when opening PDFs and downloads, so we cannot use it right now

                // and once again for IE
                /* if (
                    !string.IsNullOrWhiteSpace(csp) 
                    && !context.HttpContext.Response.Headers.ContainsKey("X-Content-Security-Policy")
                )
                {
                    context.HttpContext.Response.Headers.Add("X-Content-Security-Policy", csp);
                } */
            }
        }

        private string GetConfiguredCspHeader()
        {
            var config = GetConfigurationForKey(CspHeaderBaseConfigKey);

            if (config == null || config.Count == 0)
            {
                return null;
            }

            string cspHeader = null;

            foreach (var cspPart in config)
            {
                var name = cspPart.Key;
                var value = cspPart.Value;

                if (string.IsNullOrWhiteSpace(name) || value == null)
                {
                    continue;
                }

                var printableValue = string.IsNullOrWhiteSpace(value) ? "" : " " + value;
                cspHeader = $"{cspHeader ?? ""}{name}{printableValue}; ";
            }

            return cspHeader;
        }

        private void AddSecurityHeaders(ResultExecutingContext context)
        {
            var config = GetConfigurationForKey(SecurityHeadersBaseConfigKey);

            if (config == null || config.Count == 0)
            {
                return;
            }

            foreach (var header in config)
            {
                var name = header.Key;
                var value = header.Value;

                if (
                    !string.IsNullOrWhiteSpace(name)
                    && !string.IsNullOrWhiteSpace(value)
                    && !context.HttpContext.Response.Headers.ContainsKey(name)
                )
                {
                    context.HttpContext.Response.Headers.Add(name, value);
                }
            }
        }

        private List<KeyValuePair<string,string>> GetConfigurationForKey(string baseKey)
        {
            var allConfig = new List<KeyValuePair<string,string>>();

            if (string.IsNullOrWhiteSpace(baseKey))
            {
                return allConfig;
            }

            // tagged section
            var tagNameKey = !string.IsNullOrWhiteSpace(_tagName) ?
                baseKey.Replace($":{DefaultTagName}:", _tagName) : null;

            var configSectionForTagName =  !string.IsNullOrWhiteSpace(tagNameKey) ?
                _configuration?.GetSection(tagNameKey) :
                null;

            var taggedValues = configSectionForTagName?.AsEnumerable();
            var alreadyAddedNames = new List<string>();

            if (taggedValues != null)
            {
                foreach (var entry in taggedValues)
                {
                    var name = entry.Key.Substring(tagNameKey.Length).ToLower();
                    var value = entry.Value;

                    name = name.StartsWith(":") ? name.Substring(1) : name;
                    allConfig.Add(new KeyValuePair<string, string>(name, value));

                    alreadyAddedNames.Add(name);
                }
            }

            // default section
            var configSection = _configuration?.GetSection(baseKey);
            var defaultValues = configSection?.AsEnumerable();

            if (defaultValues != null)
            {
                foreach (var entry in defaultValues)
                {
                    var name = entry.Key.Substring(baseKey.Length).ToLower();
                    var value = entry.Value;

                    name = name.StartsWith(":") ? name.Substring(1) : name;

                    if (!alreadyAddedNames.Contains(name))
                    {
                        allConfig.Add(new KeyValuePair<string, string>(name, value));
                    }
                }
            }

            return allConfig;
        }
    }
}