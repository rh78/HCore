// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// Original at https://github.com/elastic/elasticsearch-net/blob/7.17.5/src/Nest.JsonNetSerializer/JsonReaderExtensions.cs

// Adjusted for HCore by Reinhard Holzner
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HCore.Database.Extensions
{
    public static class JsonReaderExtensions
    {
        public static JToken ReadTokenWithDateParseHandlingNone(this JsonReader reader)
        {
            var dateParseHandling = reader.DateParseHandling;

            reader.DateParseHandling = DateParseHandling.None;

            var token = JToken.ReadFrom(reader);

            reader.DateParseHandling = dateParseHandling;

            return token;
        }

        public static async Task<JToken> ReadTokenWithDateParseHandlingNoneAsync(this JsonReader reader, CancellationToken cancellationToken = default)
        {
            var dateParseHandling = reader.DateParseHandling;

            reader.DateParseHandling = DateParseHandling.None;

            var token = await JToken.ReadFromAsync(reader, cancellationToken).ConfigureAwait(false);

            reader.DateParseHandling = dateParseHandling;

            return token;
        }
    }
}
