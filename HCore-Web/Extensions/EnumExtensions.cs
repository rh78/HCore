using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace System
{
    public static class EnumExtensions
    {
        public static string ToEnumMemberAttrValue(this Enum e)
        {
            string json = JsonConvert.SerializeObject(e);

            json = json?.Replace("\"", "");

            return json;
        }

        public static T ToEnum<T>(this string enumText) where T : struct
        {
            return JsonConvert.DeserializeObject<T>("\"" + enumText + "\"");
        }
    }
}