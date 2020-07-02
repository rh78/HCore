using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HCore.Storage.Client
{
    public interface IStorageClient
    {
        Task DownloadToStreamAsync(string containerName, string fileName, Stream stream);
        
        Task<string> UploadFromStreamAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool overwriteIfExists);
        Task<string> UploadFromStreamLowLatencyProfileAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream, bool containerIsPublic);

        Task<string> GetSignedDownloadUrlAsync(string containerName, string fileName, TimeSpan validityTimeSpan, string downloadFileName = null);
    }
}
