using System.Text.Json;

namespace SengokuScroll.Common.Utils;

public static class JsonUtils
{
    private static readonly JsonSerializerOptions indentedOption = new() { WriteIndented = true };

    public static string ToJson(this object obj) => JsonSerializer.Serialize(obj);

    public static string ToJsonIndented(this object obj) => JsonSerializer.Serialize(obj, indentedOption);

    public static T? FromJson<T>(this string s) => JsonSerializer.Deserialize<T>(s);
}
