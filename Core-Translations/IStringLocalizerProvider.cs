using Microsoft.Extensions.Localization;

namespace ReinhardHolzner.Core.Translations
{
    public interface IStringLocalizerProvider
    {
        IStringLocalizer StringLocalizer { get; }
    }
}
