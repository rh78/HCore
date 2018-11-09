namespace HCore.Rest.Client
{
    public interface IRestSharpClientProvider
    {
        IRestSharpClient GetRestSharpClient(string baseUrl);
    }
}
