namespace HCore.Translations
{
    public interface ITranslationsProvider
    {
        string GetString(string key);
        string GetJson();
    }
}
