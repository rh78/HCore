using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static System.Convert;

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
        public static readonly string UrlQueryKeyName = "data";
        public static readonly string UrlQueryKeyMimeType = "mime";
        public static readonly string UrlQueryKeyFileName = "fileName";
        public static readonly string UrlQueryKeySourceUrl = "u";

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


        public Uri CreateProxyUrl(Uri downloadSourceUri, string fileName, string proxyBaseUrl, string downloadMimeType = null)
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

            var proxyUri = new Uri(proxyBaseUrl);
            var baseUri = proxyUri.GetComponents(
                UriComponents.Scheme | UriComponents.Host | UriComponents.Port | UriComponents.Path,
                UriFormat.UriEscaped
            );

            var query = QueryHelpers.ParseQuery(proxyUri.Query);

            var queryItems = query.SelectMany(
                x => x.Value,
                (col, value) => new KeyValuePair<string, string>(col.Key, value)
            ).ToList();

            var queryBuilder = new QueryBuilder(queryItems);

            queryBuilder.Add(UrlQueryKeyName, _protector.Protect(CalculateHashFromParameters(downloadSourceUri, fileName, downloadMimeType)));
            queryBuilder.Add(UrlQueryKeyMimeType, downloadMimeType);
            queryBuilder.Add(UrlQueryKeySourceUrl, downloadSourceUri.ToString());
            queryBuilder.Add(UrlQueryKeyFileName, fileName);

            return new Uri(baseUri + queryBuilder.ToQueryString());
        }

        public virtual async Task<IDownloadFileData> GetFileDataAsync(HttpRequest request, Stream inputData)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            Uri downloadUri = new Uri(request.Query[UrlQueryKeySourceUrl]);
            string fileName = request.Query[UrlQueryKeyFileName];
            string mimeType = request.Query[UrlQueryKeyMimeType];
            string characterSet = "utf-8";

            string protectedHash = request.Query[UrlQueryKeyName];
            string originalHash = _protector.Unprotect(protectedHash);

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
            return !string.IsNullOrEmpty(hashToVerify) && string.Equals(calculatedHash, hashToVerify);
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
