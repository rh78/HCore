using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch.Analysis;
using HCore.Database.ElasticSearch.Filters;

namespace HCore.Database.ElasticSearch.JsonConverters
{
    public sealed partial class ConcatenateTokenFilterConverter : JsonConverter<ITokenFilter>
    {
        public override ConcatenateTokenFilter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // TODO this still needs to be corrected (see below) - currently we see no calls for this

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            var concatenateTokenFilter = new ConcatenateTokenFilter();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return concatenateTokenFilter;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException();
                }

                var propertyName = reader.GetString();

                reader.Read();

                if (reader.TokenType == JsonTokenType.Null)
                {
                    continue;
                }

                switch (propertyName)
                {
                    case "token_separator":
                        concatenateTokenFilter.TokenSeparator = reader.GetString();

                        break;
                    case "increment_gap":
                        var incrementGapString = reader.GetString();

                        if (!string.IsNullOrEmpty(incrementGapString) && int.TryParse(incrementGapString, out var incrementGap))
                        {
                            concatenateTokenFilter.IncrementGap = incrementGap;
                        }

                        break;
                    default:
                        reader.Skip();

                        break;
                }
            }

            throw new JsonException("Unexpected end of Json.");
        }

        public override void Write(Utf8JsonWriter writer, ITokenFilter value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("type", value.Type);

            if (value is ConcatenateTokenFilter concatenateTokenFilter)
            {
                if (concatenateTokenFilter.TokenSeparator != null)
                {
                    writer.WriteString("token_separator", concatenateTokenFilter.TokenSeparator);
                }

                if (concatenateTokenFilter.IncrementGap.HasValue)
                {
                    writer.WriteNumber("increment_gap", concatenateTokenFilter.IncrementGap.Value);
                }
            }
            else if (value is EdgeNGramTokenFilter edgeNGramTokenFilter)
            {
                if (edgeNGramTokenFilter.MinGram != null)
                {
                    writer.WriteNumber("min_gram", (decimal)edgeNGramTokenFilter.MinGram);
                }

                if (edgeNGramTokenFilter.MaxGram != null)
                {
                    writer.WriteNumber("max_gram", (decimal)edgeNGramTokenFilter.MaxGram);
                }
            }
            else if (value is NGramTokenFilter ngramTokenFilter)
            {
                if (ngramTokenFilter.MinGram != null)
                {
                    writer.WriteNumber("min_gram", (decimal)ngramTokenFilter.MinGram);
                }

                if (ngramTokenFilter.MaxGram != null)
                {
                    writer.WriteNumber("max_gram", (decimal)ngramTokenFilter.MaxGram);
                }
            }
            else
            {
                throw new NotImplementedException($"{value.Type} cannot be serialized");
            }

            writer.WriteEndObject();
        }
    }
}
