using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static System.Convert;
using static Newtonsoft.Json.JsonConvert;

namespace HCore.Web.Providers.Impl
{
    /// <summary>
    /// Defines a service to facade downloads with a download proxy, which might perform some processing of downloads.
    ///
    /// <remarks>There is a need for some downloads from external sources to rename the download file name.
    /// Unfortunately this is not possible with external URLs. Only a proxy facade may do so by
    /// downloading the file data on behalf of the user and passing it with a changed name.</remarks>
    ///
    /// <remarks>More processing is possible, of course.</remarks>
    /// </summary>
    public class DownloadProcessingProxyUrlProviderImpl : IDownloadProcessingProxyUrlProvider
    {
        public static readonly string ProcessingProxyBaseUrlConfig = "DownloadProxy:BaseUrl";
        public static readonly string UrlQueryKeyName = "data";

        private readonly ILogger<DownloadProcessingProxyUrlProviderImpl> _logger;
        private readonly IDataProtector _protector;
        private readonly IServiceProvider _serviceProvider;

        public DownloadProcessingProxyUrlProviderImpl(
            IDataProtectionProvider protectionProvider,
            IServiceProvider serviceProvider,
            ILogger<DownloadProcessingProxyUrlProviderImpl> logger
        )
        {
            if (protectionProvider == null)
            {
                throw new ArgumentNullException(nameof(protectionProvider));
            }

            _protector = protectionProvider.CreateProtector(nameof(DownloadProcessingProxyUrlProviderImpl));
            _serviceProvider = serviceProvider;
            _logger = logger;
        }


        public virtual Task<Uri> CreateProxyUrlAsync(Uri downloadSourceUri, string fileName, string proxyBaseUrl, string downloadMimeType = null)
        {
            if (downloadSourceUri == null)
            {
                throw new ArgumentNullException(nameof(downloadSourceUri));
            }

            if (!downloadSourceUri.IsAbsoluteUri)
            {
                throw new ArgumentException("Download source URI is not absolute URI!");
            }

            if (string.IsNullOrEmpty(proxyBaseUrl))
            {
                throw new ArgumentException("No proxy base URL has been provided!");
            }

            // convert the data to JSON string
            string uriQueryPayload = ConvertParametersToQueryData(CalculateHashFromParameters(downloadSourceUri, fileName, downloadMimeType));

            UriBuilder proxyUri = new UriBuilder(proxyBaseUrl);
            if (proxyUri.Query != null && proxyUri.Query.Length > 1)
            {
                proxyUri.Query = proxyUri.Query.Substring(1) + "&" + uriQueryPayload;
            }
            else
            {
                proxyUri.Query = uriQueryPayload;
            }

            proxyUri.Query = proxyUri.Query.Substring(1) + "&fileName=" + Uri.EscapeDataString(fileName)
                + (downloadMimeType != null ? "&mime=" + Uri.EscapeDataString(downloadMimeType) : "")
                + "&u=" + Uri.EscapeDataString(downloadSourceUri.ToString())
            ;
            return Task.FromResult(proxyUri.Uri);
        }

        public virtual async Task<IDownloadFileData> GetFileDataAsync(HttpRequest request, Stream inputData)
        {
            Uri downloadUri = new Uri(request.Query["u"]);
            string fileName = request.Query["fileName"];
            string mimeType = request.Query["mime"];
            string characterSet = "utf-8";
            string originalHash = ConvertParametersFromQueryData(request);

            if (!IsHashValid(originalHash, downloadUri, fileName, mimeType))
            {
                throw new UnauthorizedAccessException("Invalid query data");
            }

            Stream fileData = inputData;
            if (fileData == null)
            {
                var remoteRequest = new HttpRequestMessage(HttpMethod.Get, downloadUri);
                var client = _serviceProvider.GetService<HttpClient>();
                var response = await client.SendAsync(remoteRequest).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    fileData = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                    if (string.IsNullOrEmpty(mimeType))
                    {
                        mimeType = response.Content.Headers.ContentType.MediaType;
                    }

                    characterSet = response.Content.Headers.ContentType.CharSet;
                }
                else
                {
                    _logger.LogError(
                        $"Failed to download file from source location {downloadUri}. " +
                        $"file name: {JsonConvert.SerializeObject(fileName)}. " +
                        $"HTTP result was: {response.StatusCode} {response.ReasonPhrase}, response body: {response.Content}"
                    );

                    throw new HttpRequestException("Failed to download file from source location!");
                }
            }

            return new DownloadFileDataImpl()
            {
                Data = fileData,
                FileName = fileName,
                CharacterSet = characterSet,
                MimeType = mimeType,
            };
        }

        public virtual Uri GetSourceUri(HttpRequest request)
        {
            return new Uri(request.Query["u"]);
        }

        private dynamic ConvertParametersFromQueryData(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!request.Query.TryGetValue(UrlQueryKeyName, out var queryData) || queryData.Count != 1)
            {
                return null;
            }

            string protectedJson = queryData[0];
            string json = _protector.Unprotect(protectedJson);
            return DeserializeObject<dynamic>(json);
        }

        private string ConvertParametersToQueryData(dynamic downloadParameters)
        {
            string json = SerializeObject(downloadParameters);
            string protectedJson = _protector.Protect(json);
            return $"{UrlQueryKeyName}={Uri.EscapeDataString(protectedJson)}" ;
        }

        private string CalculateHashFromParameters(Uri downloadSourceUri, string fileName, string downloadMimeType = null)
        {
            string valueToHash = $"{fileName}:{downloadMimeType}:{downloadSourceUri}";

            // see https://stackoverflow.com/questions/33245247/hashalgorithms-in-coreclr
            using var algorithm = SHA256.Create();

            // Create the at_hash using the access token returned by CreateAccessTokenAsync.
            var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(valueToHash));

            return ToBase64String(hash);
        }

        private bool IsHashValid(string hashToVerify, Uri downloadSourceUri, string fileName, string downloadMimeType = null)
        {
            string calculatedHash = CalculateHashFromParameters(downloadSourceUri, fileName, downloadMimeType);
            return string.Equals(calculatedHash, hashToVerify);
        }
    }

    internal class DownloadFileDataImpl : IDownloadFileData
    {
        public Stream Data { get; set; }
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public string CharacterSet { get; set; }
    }
}
