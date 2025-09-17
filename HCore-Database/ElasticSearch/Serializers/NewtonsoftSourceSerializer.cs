// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// Original at https://github.com/elastic/elasticsearch-net/blob/7.17.5/src/Nest.JsonNetSerializer/ConnectionSettingsAwareSerializerBase.Customization.cs

// Adjusted for HCore by Reinhard Holzner
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using HCore.Database.ElasticSearch.JsonConverters;
using HCore.Database.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HCore.Database.ElasticSearch.Serializers
{
    public class NewtonsoftSourceSerializer : Serializer
    {
        internal const int _defaultBufferSize = 1024;

        private readonly JsonSerializer _serializer;
        private readonly JsonSerializer _collapsedSerializer;

        private readonly JsonSerializerSettings _jsonSerializerSettings;

        private readonly List<JsonConverter> _defaultConverters;
        private readonly IEnumerable<JsonConverter> _jsonConverters;

        public NewtonsoftSourceSerializer(Serializer builtInSerializer, IElasticsearchClientSettings elasticsearchClientSettings, JsonSerializerSettings jsonSerializerSettings = null, IEnumerable<JsonConverter> jsonConverters = null)
        {
            _jsonSerializerSettings = jsonSerializerSettings ?? new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Include
            };

            _jsonConverters = jsonConverters ?? [];

            _defaultConverters =
            [
                new HandleTypesOnSourceJsonConverter(builtInSerializer),
                new TimeSpanToStringConverter()
            ];

            _serializer = CreateSerializer(elasticsearchClientSettings, SerializationFormatting.Indented);
            _collapsedSerializer = CreateSerializer(elasticsearchClientSettings, SerializationFormatting.None);
        }

        private JsonSerializer CreateSerializer(IElasticsearchClientSettings elasticsearchClientSettings, SerializationFormatting formatting)
        {
            _jsonSerializerSettings.ContractResolver = new ConnectionSettingsAwareContractResolver(elasticsearchClientSettings);

            _jsonSerializerSettings.Formatting = formatting == SerializationFormatting.Indented
                ? Formatting.Indented
                : Formatting.None;

            foreach (var converter in _jsonConverters.Concat(_defaultConverters))
            {
                _jsonSerializerSettings.Converters.Add(converter);
            }

            return JsonSerializer.Create(_jsonSerializerSettings);
        }

        public override object Deserialize(Type type, Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                return _serializer.Deserialize(jsonTextReader, type);
            }
        }

        public override T Deserialize<T>(Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                return _serializer.Deserialize<T>(jsonTextReader);
            }
        }

        public override async ValueTask<object> DeserializeAsync(Type type, Stream stream, CancellationToken cancellationToken = default)
        {
            using (var streamReader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                var token = await jsonTextReader.ReadTokenWithDateParseHandlingNoneAsync(cancellationToken).ConfigureAwait(false);

                return token.ToObject(type, _serializer);
            }
        }

        public override async ValueTask<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            using (var streamReader = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                var token = await jsonTextReader.ReadTokenWithDateParseHandlingNoneAsync(cancellationToken).ConfigureAwait(false);

                return token.ToObject<T>(_serializer);
            }
        }

        public override void Serialize(object data, Type type, Stream stream, SerializationFormatting formatting = SerializationFormatting.None, CancellationToken cancellationToken = default)
        {
            Serialize(data, stream, formatting);
        }

        public override void Serialize<T>(T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.None)
        {
            using (var writer = new StreamWriter(stream, JTokenExtensions.ExpectedEncoding, _defaultBufferSize, true))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                var serializer = formatting == SerializationFormatting.Indented
                    ? _serializer
                    : _collapsedSerializer;

                serializer.Serialize(jsonWriter, data);
            }
        }

        public override Task SerializeAsync(object data, Type type, Stream stream, SerializationFormatting formatting = SerializationFormatting.None, CancellationToken cancellationToken = default)
        {
            Serialize(data, stream, formatting);

            return Task.CompletedTask;
        }

        public override Task SerializeAsync<T>(T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.None, CancellationToken cancellationToken = default)
        {
            Serialize(data, stream, formatting);

            return Task.CompletedTask;
        }
    }
}
