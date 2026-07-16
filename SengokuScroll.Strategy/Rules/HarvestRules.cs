using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Rules;

/// <summary>Region 收粮日与贡赋触发判定（M4-b）。</summary>
public static class HarvestRules
{
    /// <summary>当日是否为该据点的收粮/农业税日。</summary>
    public static bool IsHarvestDay(
        Stronghold stronghold,
        GameDate date,
        IReadOnlyDictionary<int, RegionHarvestProfile> profiles,
        int regionId)
    {
        foreach (var evt in ResolveEvents(profiles, regionId))
        {
            if (evt.Month == date.Month && evt.Day == date.Day)
                return true;
        }

        return false;
    }

    /// <summary>取当日收粮事件；非收粮日返回 null。</summary>
    public static HarvestEventDefinition? ResolveTodayEvent(
        GameDate date,
        IReadOnlyDictionary<int, RegionHarvestProfile> profiles,
        int regionId)
    {
        foreach (var evt in ResolveEvents(profiles, regionId))
        {
            if (evt.Month == date.Month && evt.Day == date.Day)
                return evt;
        }

        return null;
    }

    /// <summary>收粮日跳过农业日产（改由 bulk harvest 结算）。</summary>
    public static bool ShouldSkipDailyFoodProduction(
        Stronghold stronghold,
        GameDate date,
        IReadOnlyDictionary<int, RegionHarvestProfile> profiles,
        int regionId)
        => IsHarvestDay(stronghold, date, profiles, regionId);

    /// <summary>按区域解析收粮日程；无配置时回退北方单季默认收粮日。</summary>
    private static IEnumerable<HarvestEventDefinition> ResolveEvents(
        IReadOnlyDictionary<int, RegionHarvestProfile> profiles,
        int regionId)
    {
        // 业务：剧本为区域配置了收粮日程时，按该区域的月日事件表判定
        if (regionId > 0
            && profiles.TryGetValue(regionId, out var profile)
            && profile.Events.Count > 0)
        {
            return profile.Events;
        }

        // 业务：未配置区域或无事件时，使用北方单季收粮作为全局默认
        return [HarvestConstants.DefaultNorthernSingle];
    }
}
