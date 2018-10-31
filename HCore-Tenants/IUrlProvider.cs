namespace HCore.Tenants
{
    public interface IUrlProvider : Web.Providers.IUrlProvider
    {
        string WebUrl { get; }

        string BuildWebUrl(string path);
        string BuildApiUrl(string path);
    }
}
