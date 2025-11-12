namespace HCore.Web.Providers
{
    public interface IHtmlIncludesProvider
    {
        bool Applies { get; }

        string GetHeaderIncludes(string scriptNonce);
        string GetBodyIncludes(string scriptNonce);
    }
}
