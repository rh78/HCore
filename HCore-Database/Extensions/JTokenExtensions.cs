// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

// Original at https://github.com/elastic/elasticsearch-net/tree/7.17.5

// Adjusted for HCore by Reinhard Holzner
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HCore.Database.Extensions
{
    public static class JTokenExtensions
    {
        internal const int _defaultBufferSize = 1024;

        public static readonly Encoding ExpectedEncoding = new UTF8Encoding(false);

        public static MemoryStream ToStream(this JToken token)
        {
            var memoryStream = new MemoryStream();

            using var streamWriter = new StreamWriter(memoryStream, ExpectedEncoding, _defaultBufferSize, true);
            using var jsonTextWriter = new JsonTextWriter(streamWriter);

            token.WriteTo(jsonTextWriter);

            jsonTextWriter.Flush();

            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}
