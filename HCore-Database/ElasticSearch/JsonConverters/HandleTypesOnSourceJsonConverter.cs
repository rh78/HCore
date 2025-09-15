using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using HCore.Database.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HCore.Database.ElasticSearch.JsonConverters
{
    public class HandleTypesOnSourceJsonConverter : JsonConverter
    {
        private static readonly HashSet<Type> _typesThatCanAppearInSource =
        [
            typeof(Aggregation),
            typeof(Query),
            typeof(JoinField),
            typeof(Attachment),
            typeof(GeoLocation)
        ];

        private readonly Serializer _builtInSerializer;

        public HandleTypesOnSourceJsonConverter(Serializer builtInSerializer)
        {
            _builtInSerializer = builtInSerializer;
        }

        public override bool CanRead => true;

        public override bool CanWrite => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var formatting = serializer.Formatting == Formatting.Indented
                ? SerializationFormatting.Indented
                : SerializationFormatting.None;

            using var memoryStream = new MemoryStream();
            using var streamReader = new StreamReader(memoryStream, JTokenExtensions.ExpectedEncoding);
            using var jsonTextReader = new JsonTextReader(streamReader);

            _builtInSerializer.Serialize(value, memoryStream, formatting);

            memoryStream.Position = 0;
            
            var token = jsonTextReader.ReadTokenWithDateParseHandlingNone();

            writer.WriteToken(token.CreateReader(), true);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = reader.ReadTokenWithDateParseHandlingNone();

            if (objectType == typeof(JoinField) && token.Type == JTokenType.String)
            {
                return JoinField.Root(token.Value<string>());
            }

            using var memoryStream = token.ToStream();

            return _builtInSerializer.Deserialize(objectType, memoryStream);
        }

        public override bool CanConvert(Type objectType) => _typesThatCanAppearInSource.Contains(objectType);
    }
}
