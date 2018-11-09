using Microsoft.Extensions.Localization;
using HCore.Translations.Resources;
using System.Reflection;

namespace HCore.Translations.Providers.Impl
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
