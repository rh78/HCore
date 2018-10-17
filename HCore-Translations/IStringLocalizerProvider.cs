using Microsoft.Extensions.Localization;

namespace HCore.Translations
{
    public interface IStringLocalizerProvider
    {
        IStringLocalizer StringLocalizer { get; }
    }
}
