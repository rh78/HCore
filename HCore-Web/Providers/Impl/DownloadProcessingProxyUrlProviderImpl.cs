using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
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

        private readonly IHttpClientFactory _httpClientFactory;

        public DownloadProcessingProxyUrlProviderImpl(
            IHttpClientFactory httpClientFactory,
            ILogger<DownloadProcessingProxyUrlProviderImpl> logger
        )
        {
            _httpClientFactory = httpClientFactory;

            _logger = logger;
        }


        public Uri CreateProxyUrl(X509Certificate2 signingCertificate, Uri downloadSourceUri, string fileName, string proxyBaseUrl, string downloadMimeType = null)
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

            byte[] originalHash = CalculateHashFromParameters(downloadSourceUri, fileName, downloadMimeType);

            byte[] protectedHash;

            using (var rsa = (RSACng)signingCertificate.GetRSAPublicKey())
            {
                protectedHash = rsa.Encrypt(originalHash, RSAEncryptionPadding.OaepSHA1);
            }

            string protectedHashBase64 = ToBase64String(protectedHash);

            queryBuilder.Add(UrlQueryKeyName, protectedHashBase64);
            queryBuilder.Add(UrlQueryKeyMimeType, downloadMimeType);
            queryBuilder.Add(UrlQueryKeySourceUrl, downloadSourceUri.ToString());
            queryBuilder.Add(UrlQueryKeyFileName, fileName);

            return new Uri(baseUri + queryBuilder.ToQueryString());
        }

        public virtual async Task<IDownloadFileData> GetFileDataAsync(X509Certificate2 signingCertificate, HttpRequest request, Stream inputData = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            Uri downloadUri = new Uri(request.Query[UrlQueryKeySourceUrl]);
            string fileName = request.Query[UrlQueryKeyFileName];
            string mimeType = request.Query[UrlQueryKeyMimeType];
            string characterSet = "utf-8";

            string protectedHashBase64 = request.Query[UrlQueryKeyName];

            byte[] protectedHash = FromBase64String(protectedHashBase64);

            byte[] originalHash;

            using (var rsa = (RSACng)signingCertificate.GetRSAPrivateKey())
            {
                originalHash = rsa.Decrypt(protectedHash, RSAEncryptionPadding.OaepSHA1);
            }

            if (!IsHashValid(originalHash, downloadUri, fileName, mimeType))
            {
                throw new UnauthorizedAccessException("Invalid query data");
            }

            Stream fileData = inputData;
            long? contentLength = null;

            if (fileData == null)
            {
                var httpClient = _httpClientFactory.CreateClient();

                httpClient.Timeout = TimeSpan.FromMinutes(1);

                using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, downloadUri))
                {
                    HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        _logger.LogError(
                            $"Failed to download file from source location {downloadUri}. " +
                            $"file name: {JsonConvert.SerializeObject(fileName)}. " +
                            $"HTTP result was: {responseMessage.StatusCode} {responseMessage.ReasonPhrase}, response body: {responseMessage.Content}"
                        );

                        throw new HttpRequestException("Failed to download file from source location");
                    }

                    var contentDispositionHeader = responseMessage.Content.Headers.ContentDisposition;

                    string originalMediaType = responseMessage.Content?.Headers?.ContentType?.MediaType;
                    string originalCharacterSet = responseMessage.Content?.Headers?.ContentType?.CharSet;

                    contentLength = responseMessage.Content?.Headers?.ContentLength;

                    if (string.IsNullOrEmpty(mimeType))
                        mimeType = originalMediaType;

                    if (!string.IsNullOrEmpty(originalCharacterSet))
                        characterSet = originalCharacterSet;

                    fileData = await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
                }
            }

            return new DownloadFileDataImpl()
            {
                Data = fileData,
                FileName = fileName,
                CharacterSet = characterSet,
                ContentLength = contentLength,
                MimeType = mimeType,                
            };
        }

        private byte[] CalculateHashFromParameters(Uri downloadSourceUri, string fileName, string downloadMimeType = null)
        {
            string valueToHash = $"{fileName}:{downloadMimeType}:{downloadSourceUri}";

            // see https://stackoverflow.com/questions/33245247/hashalgorithms-in-coreclr
            using var algorithm = SHA256.Create();

            // Create the at_hash using the access token returned by CreateAccessTokenAsync.
            var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(valueToHash));

            return hash;
        }

        private bool IsHashValid(byte[] hashToVerify, Uri downloadSourceUri, string fileName, string downloadMimeType = null)
        {
            byte[] calculatedHash = CalculateHashFromParameters(downloadSourceUri, fileName, downloadMimeType);

            return hashToVerify.SequenceEqual(calculatedHash);
        }
    }

    internal class DownloadFileDataImpl : IDownloadFileData
    {
        public Stream Data { get; set; }
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public string CharacterSet { get; set; }
        public long? ContentLength { get; set; }
    }
}
