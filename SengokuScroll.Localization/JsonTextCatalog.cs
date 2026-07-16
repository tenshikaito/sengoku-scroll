using System.Reflection;
using System.Text.Json;
using SengokuScroll.Localization.Abstractions;

namespace SengokuScroll.Localization;

/// <summary>从嵌入 JSON 资源加载译文表。</summary>
public sealed class JsonTextCatalog : ITextCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly Dictionary<string, string> entries;

    public JsonTextCatalog(string culture, IReadOnlyDictionary<string, string> entries)
    {
        Culture = culture;
        this.entries = new Dictionary<string, string>(entries, StringComparer.Ordinal);
    }

    public string Culture { get; }

    public bool TryGet(string key, out string? value)
        => entries.TryGetValue(key, out value);

    public IReadOnlyDictionary<string, string> AsReadOnly() => entries;

    /// <summary>加载嵌入资源 <c>Resources/{culture}.json</c>。</summary>
    public static JsonTextCatalog LoadEmbedded(Assembly assembly, string culture)
    {
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith($"Resources.{culture}.json", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
            return new JsonTextCatalog(culture, ReadDictionary("{}"));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"无法读取嵌入资源 {resourceName}");

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return new JsonTextCatalog(culture, ReadDictionary(json));
    }

    private static IReadOnlyDictionary<string, string> ReadDictionary(string json)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
        return raw ?? new Dictionary<string, string>();
    }
}
