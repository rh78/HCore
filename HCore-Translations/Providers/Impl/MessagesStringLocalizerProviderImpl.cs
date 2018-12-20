using Microsoft.Extensions.Localization;
using HCore.Translations.Resources;
using System.Reflection;

namespace HCore.Translations.Providers.Impl
{
    internal class MessagesStringLocalizerProviderImpl : IStringLocalizerProvider
    {
        public IStringLocalizer StringLocalizer { get; }

        public MessagesStringLocalizerProviderImpl(IStringLocalizerFactory stringLocalizerFactory)
        {
            var type = typeof(Messages);
            var assemblyName = new AssemblyName(type.GetTypeInfo().Assembly.FullName);
            
            StringLocalizer = stringLocalizerFactory.Create("Messages", assemblyName.Name);
        }
    }
}
