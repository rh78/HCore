namespace HCore.Tenants.Providers
{
    public interface INonHttpContextUrlProvider : Web.Providers.INonHttpContextUrlProvider
    {
        string WebUrl { get; }

        string BuildWebUrl(string path);

        string BuildEcbBackendApiUrl(string path);
        string BuildPortalsBackendApiUrl(string path);

        string BuildFrontendApiUrl(string path);        
    }
}
