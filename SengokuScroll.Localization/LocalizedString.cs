using SengokuScroll.Localization.Abstractions;

namespace SengokuScroll.Localization;

/// <summary>延迟解析的本地化字符串（业务层存 key + 参数，展示层再 Resolve）。</summary>
public sealed record LocalizedString(string Key, object?[]? Args = null)
{
    public string Resolve(ITextLocalizer localizer)
        => Args is { Length: > 0 }
            ? localizer.Format(Key, Args)
            : localizer.GetString(Key);

    public override string ToString() => Key;
}
