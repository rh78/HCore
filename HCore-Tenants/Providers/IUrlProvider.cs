namespace HCore.Tenants.Providers
{
    public interface IUrlProvider : Web.Providers.IUrlProvider
    {
        string WebUrl { get; }

        string BuildWebUrl(string path);

        string BuildEcbBackendApiUrl(string path);
        string BuildPortalsBackendApiUrl(string path);

        string BuildFrontendApiUrl(string path);
    }
}
