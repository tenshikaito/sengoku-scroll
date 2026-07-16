namespace SengokuScroll.Localization.Abstractions;

/// <summary>某一文化下的 key→译文表。</summary>
public interface ITextCatalog
{
    string Culture { get; }

    bool TryGet(string key, out string? value);

    IReadOnlyDictionary<string, string> AsReadOnly();
}
