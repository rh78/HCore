using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Linq;

namespace System
{
    public static class EnumExtensions
    {
        private static List<JsonConverter> converters = new JsonConverter[] { new StringEnumConverter() }.ToList();

        private static JsonConverter StringEnumConverter()
        {
            throw new NotImplementedException();
        }

        public static string ToEnumMemberAttrValue(this Enum e)
        {
            string json = JsonConvert.SerializeObject(e, new JsonSerializerSettings()
            {
                Converters = converters
            });

            json = json?.Replace("\"", "");

            return json;
        }

        public static T ToEnum<T>(this string enumText) where T : struct
        {
            return JsonConvert.DeserializeObject<T>("\"" + enumText + "\"");
        }
    }
}