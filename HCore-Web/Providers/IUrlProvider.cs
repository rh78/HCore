namespace HCore.Web.Providers
{
    public interface IUrlProvider
    {
        string BaseUrl { get; }
        
        string BuildUrl(string path);        
    }
}
