using HCore.Tenants.Models;
using HCore.Tenants.Models.Impl;
using MessagePack;
using MessagePack.Formatters;

namespace HCore.Tenants.Providers.Impl
{
    internal class TenantInfoMessagePackFormatterImpl : IMessagePackFormatter<ITenantInfo>
    {
        public void Serialize(ref MessagePackWriter writer, ITenantInfo value, MessagePackSerializerOptions options)
        {
            options.Resolver.GetFormatterWithVerify<TenantInfoImpl>().Serialize(ref writer, value as TenantInfoImpl, options);
        }

        public ITenantInfo Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var tenantInfoImpl = options.Resolver.GetFormatterWithVerify<TenantInfoImpl>().Deserialize(ref reader, options);

            return tenantInfoImpl;
        }
    }
}
