using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Rules;

/// <summary>作型：区域、气候（区域 CropPattern）与据点技术的有效组合。</summary>
public static class AgricultureCropRules
{
    public const string Single = "Single";
    public const string Double = "Double";
    public const string Triple = "Triple";

    /// <summary>区域 CropPattern 决定的气候/区域上限。</summary>
    public static string ResolveRegionCropPattern(IReadOnlyDictionary<int, RegionHarvestProfile> profiles, int regionId)
    {
        if (regionId > 0 && profiles.TryGetValue(regionId, out var profile))
        {
            var count = profile.Events.Count;
            if (count >= 3)
                return Triple;
            if (count == 2)
                return Double;
        }

        return Single;
    }

    /// <summary>据点有效作型 = min(区域, 技术)。</summary>
    public static string ResolveEffectiveCropPattern(Stronghold stronghold, string regionCropPattern)
    {
        var techCeiling = stronghold.Agriculture.KnowsTripleCrop ? Triple
            : stronghold.Agriculture.KnowsDoubleCrop ? Double
            : Single;

        return MinCropPattern(regionCropPattern, techCeiling);
    }

    public static int ResolveActiveCycleCount(string effectiveCropPattern)
        => effectiveCropPattern switch
        {
            Triple => 3,
            Double => 2,
            _ => 1
        };

    public static void InitializeForStronghold(Stronghold stronghold, string regionCropPattern)
    {
        stronghold.Agriculture ??= new StrongholdAgricultureState();
        stronghold.Agriculture.KnowsDoubleCrop = regionCropPattern is Double or Triple;
        stronghold.Agriculture.KnowsTripleCrop = regionCropPattern is Triple;
    }

    public static int ResolveCycleIndex(
        IReadOnlyList<HarvestEventDefinition> events,
        HarvestEventDefinition harvestEvent)
    {
        for (var i = 0; i < events.Count; i++)
        {
            var evt = events[i];
            if (evt.Month == harvestEvent.Month && evt.Day == harvestEvent.Day)
                return i;
        }

        return 0;
    }

    public static IReadOnlyList<CropGrowthPhaseDefinition> ResolveGrowthPhases(string effectiveCropPattern)
        => effectiveCropPattern switch
        {
            Double => DoubleCropPhases,
            Triple => TripleCropPhases,
            _ => SingleCropPhases
        };

    public static CropGrowthPhaseDefinition? ResolveActivePhase(
        GameDate date,
        string effectiveCropPattern)
    {
        foreach (var phase in ResolveGrowthPhases(effectiveCropPattern))
        {
            if (IsDateInRange(date.Month, date.Day, phase))
                return phase;
        }

        return null;
    }

    private static bool IsDateInRange(int month, int day, CropGrowthPhaseDefinition phase)
    {
        var start = phase.StartMonth * 100 + phase.StartDay;
        var end = phase.EndMonth * 100 + phase.EndDay;
        var current = month * 100 + day;
        if (start <= end)
            return current >= start && current <= end;

        return current >= start || current <= end;
    }

    private static string MinCropPattern(string a, string b)
    {
        var rankA = Rank(a);
        var rankB = Rank(b);
        return rankA <= rankB ? a : b;
    }

    private static int Rank(string pattern)
        => pattern switch
        {
            Triple => 3,
            Double => 2,
            _ => 1
        };

    private static readonly CropGrowthPhaseDefinition[] SingleCropPhases =
    [
        new(0, 4, 1, 5, 31, 4000),
        new(0, 6, 1, 8, 31, 10_000),
        new(0, 9, 1, 10, 15, 6000)
    ];

    private static readonly CropGrowthPhaseDefinition[] DoubleCropPhases =
    [
        new(0, 3, 1, 4, 30, 4000),
        new(0, 5, 1, 5, 31, 10_000),
        new(1, 6, 2, 7, 31, 5000),
        new(1, 8, 1, 8, 31, 10_000)
    ];

    private static readonly CropGrowthPhaseDefinition[] TripleCropPhases =
    [
        new(0, 3, 1, 4, 30, 4000),
        new(0, 5, 1, 5, 31, 9000),
        new(1, 6, 2, 7, 31, 5000),
        new(1, 8, 1, 8, 31, 9000),
        new(2, 9, 2, 10, 15, 7000)
    ];
}
