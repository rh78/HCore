using System.Collections.Generic;

namespace HCore.Translations
{
    public interface ITranslationsProvider
    {
        string GetString(string key);
        string GetJson();
    }
}
