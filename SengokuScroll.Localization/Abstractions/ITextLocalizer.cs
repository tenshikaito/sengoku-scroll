namespace SengokuScroll.Localization.Abstractions;

/// <summary>文本本地化器：按 key 取译文，支持占位符格式化。</summary>
public interface ITextLocalizer
{
    /// <summary>当前生效的文化标识（如 zh-CN、en-US）。</summary>
    string Culture { get; }

    /// <summary>获取译文；缺失 key 时返回 <paramref name="key"/> 本身。</summary>
    string GetString(string key);

    /// <summary>带占位符格式化（{0}、{Name} 等与 <see cref="string.Format"/> 一致）。</summary>
    string Format(string key, params object?[] args);

    /// <summary>尝试获取译文。</summary>
    bool TryGetString(string key, out string? value);
}
