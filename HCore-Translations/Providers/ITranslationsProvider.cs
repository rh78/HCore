using System;

namespace HCore.Translations.Providers
{
    public interface ITranslationsProvider
    {
        string GetString(string key);
        string GetJson();

        string TranslateError(string errorCode, string errorMessage, string uuid, string name);
    }
}
