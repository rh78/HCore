namespace HCore.Tenants.Providers
{
    public interface INonHttpContextUrlProvider : Web.Providers.INonHttpContextUrlProvider
    {
        string WebUrl { get; }

        string BuildWebUrl(string path);

        string BuildBackendApiUrl(string path);
        string BuildFrontendApiUrl(string path);        
    }
}
