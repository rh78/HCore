namespace HCore.Tenants.Providers
{
    public interface INonHttpContextUrlProvider : Web.Providers.INonHttpContextUrlProvider
    {
        string WebUrl { get; }

        string BuildWebUrl(string path);
        string BuildApiUrl(string path);
    }
}
