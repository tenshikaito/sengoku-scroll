using SengokuScroll.Domain;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>从 MasterData 解析文化/信仰显示名。</summary>
public static class CultureReligionDisplayHelper
{
    public static string ResolveCultureName(GameMasterData masterData, int cultureId, string? fallback = null)
    {
        if (cultureId > 0 && masterData.Cultures.TryGetValue(cultureId, out var culture))
            return culture.Name;

        return string.IsNullOrWhiteSpace(fallback) ? "—" : fallback.Trim();
    }

    public static string ResolveReligionName(GameMasterData masterData, int religionId, string? fallback = null)
    {
        if (religionId > 0 && masterData.Religions.TryGetValue(religionId, out var religion))
            return religion.Name;

        return string.IsNullOrWhiteSpace(fallback) ? "—" : fallback.Trim();
    }
}
