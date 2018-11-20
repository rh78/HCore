using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HCore.Web.Json
{
    internal class IgnoreOutboundNullValuesContractResolver : DefaultContractResolver
    {
        public new static readonly IgnoreOutboundNullValuesContractResolver Instance = new IgnoreOutboundNullValuesContractResolver();

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);

            foreach(var property in properties)
            {                
                property.ShouldSerialize =
                   instance =>
                   {
                       var value = property.ValueProvider.GetValue(instance);

                       return value != null;
                   };
            }

            return properties;
        }
    }
}
