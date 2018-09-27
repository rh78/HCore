namespace ReinhardHolzner.Core.Rest
{
    public interface IRestSharpClientProvider
    {
        IRestSharpClient GetRestSharpClient();
    }
}
