namespace HCore.Web.Providers
{
    public interface IHtmlTemplateFileIncludesProviderCustomProcessor
    {
        string ProcessHtml(string htmlFilePath, string html);
    }
}
