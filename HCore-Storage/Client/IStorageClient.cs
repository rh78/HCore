using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HCore.Storage.Client
{
    public interface IStorageClient
    {
        Task DownloadToStreamAsync(string containerName, string fileName, Stream stream);
        
        Task<string> UploadFromStreamAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool overwriteIfExists, IProgress<long> progressHandler = null, string downloadFileName = null);
        Task<string> UploadFromStreamLowLatencyProfileAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool containerIsPublic, IProgress<long> progressHandler = null, string downloadFileName = null);

        Task<long> GetFileSizeAsync(string containerName, string fileName);

        Task<string> GetSignedDownloadUrlAsync(string containerName, string fileName, TimeSpan validityTimeSpan, string downloadFileName = null);
        
        Task CreateContainerAsync(string containerName, bool isPublic);
        Task DeleteContainerAsync(string containerName);

        Task DeleteFileAsync(string containerName, string fileName);

        IAsyncEnumerable<string> GetStorageFileNamesAsync(string containerName, int? pageSize = null);
        Task<long> GetStorageFileSizeAsync(string containerName);
    }
}
