namespace HCore.Translations.Providers
{
    public interface ITranslationsProviderExtension
    {
        string GetString(string key);

        string GetJson();
    }
}
