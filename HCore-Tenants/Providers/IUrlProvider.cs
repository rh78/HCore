namespace HCore.Tenants.Providers
{
    public interface IUrlProvider : Web.Providers.IUrlProvider
    {
        string WebUrl { get; }

        string BuildWebUrl(string path);
        string BuildApiUrl(string path);
    }
}
