using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HCore.Storage.Client
{
    public interface IStorageClient
    {
        Task UploadFromStreamAsync(string containerName, string fileName, string mimeType, Dictionary<string, string> additionalHeaders, Stream stream);
    }
}
