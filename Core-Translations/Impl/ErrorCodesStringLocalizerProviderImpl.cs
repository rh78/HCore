using Microsoft.Extensions.Localization;
using ReinhardHolzner.Core.Translations.Resources;
using System.Reflection;

namespace ReinhardHolzner.Core.Translations.Impl
{
    internal class ErrorCodesStringLocalizerProviderImpl : IStringLocalizerProvider
    {
        public IStringLocalizer StringLocalizer { get; }

        public ErrorCodesStringLocalizerProviderImpl(IStringLocalizerFactory stringLocalizerFactory)
        {
            var type = typeof(ErrorCodes);
            var assemblyName = new AssemblyName(type.GetTypeInfo().Assembly.FullName);
            
            StringLocalizer = stringLocalizerFactory.Create("ErrorCodes", assemblyName.Name);
        }
    }
}
