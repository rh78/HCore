using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HCore.Web.Providers
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
    public interface IDownloadProcessingProxyUrlProvider
    {
        /// <summary>
        /// Takes an external URI and creates a new one that utilizes the processing proxy downloader.
        ///
        /// <remarks>Somehow the external source URI must be passed to the download processing proxy. It would be
        /// best to use .NET core data protector to do so and use data protector with the companion proxy function
        /// to decode the data back to original source URI.</remarks>
        ///
        /// <remarks>More processing is possible, of course.</remarks>
        /// </summary>
        /// <param name="downloadSourceUri">The source URI to download the file data from.</param>
        /// <param name="fileName">The file name to use for downloading.</param>
        /// <param name="proxyBaseUrl">The base URL of the proxy controller receiving the reverse proxy request.</param>
        /// <param name="downloadMimeType">(optional) the mime type to set for the download file.</param>
        /// <returns>An URI to be passed to clients that will download the file from the processing proxy.</returns>
        public Uri CreateProxyUrl(Uri downloadSourceUri, string fileName, string proxyBaseUrl, string downloadMimeType = null);

        /// <summary>
        /// Downloads the original file data based on the request data, processes the file data and configures the
        /// response accordingly.
        ///
        /// <remarks>For better stacked processing, the output is not directly written to the response
        /// but returned as a stream. Then this function can be stacked with other download processors to form some
        /// sort of processing pipe.</remarks>
        /// </summary>
        /// <param name="request">The HTTP request to read request data from.</param>
        /// <param name="inputData">(optional) Contains the file data as processed by the previous stage. If
        /// <code>null</code>, then it will be returned unchanged.</param>
        /// <returns>The file data as a stream to be piped to the next processing stage.</returns>
        public Task<IDownloadFileData> GetFileDataAsync(HttpRequest request, Stream inputData);
    }

    public interface IDownloadFileData
    {
        public Stream Data { get; }
        public string FileName { get; }
        public string MimeType { get; }
        public string CharacterSet { get; }
    }
}
