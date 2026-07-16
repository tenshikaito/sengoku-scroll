using SengokuScroll.Localization.Abstractions;

namespace SengokuScroll.Localization;

/// <summary>按文化回退链解析 key 的本地化器（当前文化 → zh-CN → key 本身）。</summary>
public sealed class TextLocalizer : ITextLocalizer
{
    public const string DefaultCulture = "zh-CN";
    public const string FallbackCulture = "en-US";

    private readonly ICultureContext cultureContext;
    private readonly IReadOnlyDictionary<string, ITextCatalog> catalogsByCulture;

    public TextLocalizer(ICultureContext cultureContext, IEnumerable<ITextCatalog> catalogs)
    {
        this.cultureContext = cultureContext;
        catalogsByCulture = catalogs.ToDictionary(c => c.Culture, StringComparer.OrdinalIgnoreCase);
    }

    public string Culture => cultureContext.Current.Name;

    public string GetString(string key)
    {
        if (TryResolve(key, out var value))
            return value!;

        return key;
    }

    public string Format(string key, params object?[] args)
        => string.Format(GetString(key), args);

    public bool TryGetString(string key, out string? value)
        => TryResolve(key, out value);

    private bool TryResolve(string key, out string? value)
    {
        foreach (var cultureName in BuildFallbackChain())
        {
            if (catalogsByCulture.TryGetValue(cultureName, out var catalog)
                && catalog.TryGet(key, out value))
                return true;
        }

        value = null;
        return false;
    }

    private IEnumerable<string> BuildFallbackChain()
    {
        var current = cultureContext.Current.Name;
        yield return current;

        if (!string.Equals(current, DefaultCulture, StringComparison.OrdinalIgnoreCase))
            yield return DefaultCulture;

        if (!string.Equals(current, FallbackCulture, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(DefaultCulture, FallbackCulture, StringComparison.OrdinalIgnoreCase))
            yield return FallbackCulture;
    }
}
