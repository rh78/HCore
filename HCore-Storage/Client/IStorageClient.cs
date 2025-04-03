using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HCore.Storage.Models;

namespace HCore.Storage.Client
{
    public interface IStorageClient
    {
        Task DownloadToStreamAsync(string containerName, string fileName, Stream stream);

        Task<string> UploadChunkFromStreamAsync(string containerName, string fileName, long blockId, long blockStart, Stream blockStream, bool overwriteIfExists, IProgress<long> progressHandler = null);
        Task<string> FinalizeChunksAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, List<long> blockIds, bool overwriteIfExists, string downloadFileName = null);

        Task<string> UploadFromStreamAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool overwriteIfExists, IProgress<long> progressHandler = null, string downloadFileName = null);
        Task<string> UploadFromStreamLowLatencyProfileAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool containerIsPublic, IProgress<long> progressHandler = null, string downloadFileName = null);

        Task<long> GetFileSizeAsync(string containerName, string fileName);

        Task<string> GetSignedDownloadUrlAsync(string containerName, string fileName, TimeSpan validityTimeSpan, string downloadFileName = null);
        
        Task CreateContainerAsync(string containerName, bool isPublic);
        Task DeleteContainerAsync(string containerName);

        Task DeleteFileAsync(string containerName, string fileName);

        Task<ICollection<string>> GetStorageFileNamesAsync(string containerName);
        Task<long> GetStorageFileSizeAsync(string containerName);

        IAsyncEnumerable<StorageItemModel> GetStorageItemsAsync(string containerName, int? pageSize = null);
    }
}
