namespace ReinhardHolzner.Core.RestSharp
{
    public interface IRestSharpClientProvider
    {
        IRestSharpClient GetRestSharpClient();
    }
}
