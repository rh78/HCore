using Newtonsoft.Json;

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