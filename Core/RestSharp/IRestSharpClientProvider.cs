using ReinhardHolzner.HCore.RestSharp;

namespace ReinhardHolzner.Core.RestSharp
{
    public interface IRestSharpClientProvider
    {
        IRestSharpClient GetRestSharpClient();
    }
}
