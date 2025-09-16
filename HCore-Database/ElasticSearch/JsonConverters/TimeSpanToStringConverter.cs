// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// Original at https://github.com/elastic/elasticsearch-net/tree/7.17.5

// Adjusted for HCore by Reinhard Holzner
using System;
using Newtonsoft.Json;

namespace HCore.Database.ElasticSearch.JsonConverters
{
    public class TimeSpanToStringConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                var timeSpan = (TimeSpan)value;

                writer.WriteValue(timeSpan.Ticks);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return reader.TokenType switch
            {
                JsonToken.Null => null,
                JsonToken.String => TimeSpan.Parse((string)reader.Value),
                JsonToken.Integer => new TimeSpan((long)reader.Value),
                _ => throw new JsonSerializationException($"Cannot convert token of type {reader.TokenType} to {objectType}."),
            };
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(TimeSpan) || objectType == typeof(TimeSpan?);
    }
}
