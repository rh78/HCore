using Microsoft.Extensions.Localization;
using HCore.Translations.Providers;
using System.Reflection;
using HCore.Identity.Resources;

namespace HCore.Identity.Providers.Impl
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
