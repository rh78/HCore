using HCore.Storage.Client;

namespace HCore.Storage.Providers
{
    public interface IStorageClientProvider
    {
        IStorageClient GetStorageClient();
    }
}
