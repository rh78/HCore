using Microsoft.Extensions.Localization;

namespace HCore.Translations.Providers
{
    public interface IStringLocalizerProvider
    {
        IStringLocalizer StringLocalizer { get; }
    }
}
