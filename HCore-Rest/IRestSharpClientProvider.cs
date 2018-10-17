namespace HCore.Rest
{
    public interface IRestSharpClientProvider
    {
        IRestSharpClient GetRestSharpClient(string baseUrl);
    }
}
