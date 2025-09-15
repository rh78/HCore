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
