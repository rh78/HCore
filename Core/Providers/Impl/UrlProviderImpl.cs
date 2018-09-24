using Microsoft.Extensions.Configuration;
using System;

namespace ReinhardHolzner.HCore.Providers.Impl
{
    class UrlProviderImpl : IUrlProvider
    {
        public string ApiDomain { get; private set; }
        public string WebDomain { get; private set; }

        public UrlProviderImpl(IConfiguration configuration)
        {
            try
            {
                int apiPort = configuration.GetValue<int>("WebServer:ApiPort");
                string apiDomain = configuration["WebServer:ApiDomain"];

                ApiDomain = getProtocol(apiPort);
                ApiDomain += apiDomain;

                ApiDomain += getPort(apiPort);
                ApiDomain += "/";
            } catch (Exception)
            {
                ApiDomain = null;
            }

            try
            {
                int webPort = configuration.GetValue<int>("WebServer:WebPort");
                string webDomain = configuration["WebServer:WebDomain"];

                WebDomain = getProtocol(webPort);
                WebDomain += webDomain;

                WebDomain += getPort(webPort);
                WebDomain += "/";
            }
            catch (Exception)
            {
                WebDomain = null;
            }
        }

        private string getProtocol(int apiPort)
        {
            if (apiPort == 80)
                return "http://";
            else
                return "https://";
        }

        private string getPort(int apiPort)
        {
            if (apiPort != 80 && apiPort != 443)
                return $":{apiPort}";

            return "";
        }

        public string BuildApiUrl(string path)
        {
            if (string.IsNullOrEmpty(ApiDomain))
                throw new Exception("No API domain is set up for this service");

            return ApiDomain + path;
        }

        public string BuildWebUrl(string path)
        {
            if (string.IsNullOrEmpty(WebDomain))
                throw new Exception("No web domain is set up for this service");

            return WebDomain + path;
        }
    }
}
