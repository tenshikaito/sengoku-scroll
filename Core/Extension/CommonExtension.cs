using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Core.Extension
{
    public static class CommonExtension
    {
        public static string toJson(this object o, bool isIndented = false) => JsonConvert.SerializeObject(o, isIndented ? Formatting.Indented : Formatting.None);

        public static T fromJson<T>(this string s) => JsonConvert.DeserializeObject<T>(s);

        public static int getMaxId<T>(this Dictionary<int, T> map, int returnIdIfNull) => map.Any() ? map.Keys.Max() : returnIdIfNull;
    }
}
