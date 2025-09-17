// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// Original at https://github.com/elastic/elasticsearch-net/blob/7.17.5/src/Nest.JsonNetSerializer/ConnectionSettingsAwareContractResolver.cs

// Adjusted for HCore by Yosif Velev
using System;
using System.Reflection;
using Elastic.Clients.Elasticsearch;
using Elasticsearch.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace HCore.Database.ElasticSearch.Serializers
{
    public class ConnectionSettingsAwareContractResolver : DefaultContractResolver
    {
        private readonly IElasticsearchClientSettings _elasticsearchClientSettings;

        public ConnectionSettingsAwareContractResolver(IElasticsearchClientSettings elasticsearchClientSettings)
        {
            _elasticsearchClientSettings = elasticsearchClientSettings;
        }

        protected override string ResolvePropertyName(string fieldName) =>
            _elasticsearchClientSettings.DefaultFieldNameInferrer != null
                ? _elasticsearchClientSettings.DefaultFieldNameInferrer(fieldName)
                : base.ResolvePropertyName(fieldName);

        protected override JsonContract CreateContract(Type objectType)
        {
            var contract = base.CreateContract(objectType);

            if (objectType.IsEnum && objectType.GetCustomAttribute<StringEnumAttribute>() != null)
            {
                contract.Converter = new StringEnumConverter();
            }

            return contract;
        }
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            ApplyPropertyOverrides(member, property);

            return property;
        }

        /// <summary> Renames/Ignores a property based on the connection settings mapping or custom attributes for the property </summary>
        private void ApplyPropertyOverrides(MemberInfo member, JsonProperty property)
        {
            if (!_elasticsearchClientSettings.PropertyMappings.TryGetValue(member, out var propertyMapping))
            {
                if (_elasticsearchClientSettings.PropertyMappingProvider == null)
                {
                    return;
                }

                propertyMapping = _elasticsearchClientSettings.PropertyMappingProvider.CreatePropertyMapping(member);
            }

            var nameOverride = propertyMapping.Name;

            if (!string.IsNullOrWhiteSpace(nameOverride))
            {
                property.PropertyName = nameOverride;
            }

            property.Ignored = propertyMapping.Ignore;
        }
    }
}
