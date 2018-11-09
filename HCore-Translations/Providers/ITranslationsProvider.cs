using System.Collections.Generic;

namespace HCore.Translations.Providers
{
    public interface ITranslationsProvider
    {
        string GetString(string key);
        string GetJson();
    }
}
