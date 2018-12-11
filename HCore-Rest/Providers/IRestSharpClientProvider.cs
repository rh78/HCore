using HCore.Rest.Client;

namespace HCore.Rest.Providers
{
    public interface IRestSharpClientProvider
    {
        IRestSharpClient GetRestSharpClient(string baseUrl);
    }
}
