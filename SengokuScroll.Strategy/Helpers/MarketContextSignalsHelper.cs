using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>据点市场日更上下文：信号 + 可写库存/砸单依赖。</summary>
public sealed class MarketAiDayContext
{
    public required GameWorld World { get; init; }

    public required GameData GameData { get; init; }

    public required StrategyScenarioMeta ScenarioMeta { get; init; }

    public required Diagnostics.MerchantTaxLedger TaxLedger { get; init; }

    /// <summary>日更首轮允许仓位抢筹砸单；Refresh 重挂时必须为 false 防递归。</summary>
    public bool AllowOpportunisticSmash { get; init; }
}

/// <summary>战争/收粮/价崩等市场情绪信号。</summary>
public readonly record struct StrongholdMarketSignals(
    int EnemyForceCount,
    bool EnemyNearStronghold,
    bool IsBlockaded,
    bool IsHarvestDay,
    bool HarvestFearWindow,
    bool PriceCrashObserved,
    bool PriceRallyObserved,
    int HoardBiasBp,
    int DumpBiasBp);

/// <summary>从外交、威胁、收粮日历与盘口推导市场情绪。</summary>
public static class MarketContextSignalsHelper
{
    public static StrongholdMarketSignals Resolve(
        Stronghold stronghold,
        MarketAiDayContext context,
        MarketCommodityType commodity = MarketCommodityType.Food)
    {
        var gameData = context.GameData;
        var date = gameData.GameDate;
        var enemyCount = 0;
        if (gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
        {
            enemyCount = force.Diplomacies.Count(d =>
                d.Relation == Diplomacy.DiplomacyRelation.Enemy);
        }

        var enemyNear = HasNearbyHostileMilitary(stronghold, gameData);
        var blockaded = GarrisonBehaviorRules.IsStrongholdBlockaded(stronghold, gameData);

        var regionId = RegionLocationHelper.ResolveRegionId(context.World, stronghold.Location);
        var harvestDay = HarvestRules.IsHarvestDay(
            stronghold,
            date,
            context.ScenarioMeta.RegionHarvestProfiles,
            regionId);
        var harvestFear = harvestDay
            || IsWithinHarvestLeadWindow(
                stronghold,
                date,
                context.ScenarioMeta.RegionHarvestProfiles,
                regionId);

        var fair = MarketMakerAiHelper.ResolveFairReferencePrice(stronghold, commodity);
        var bestAsk = MarketMakerAiHelper.ResolveBestAsk(stronghold, commodity);
        var bestBid = MarketMakerAiHelper.ResolveBestBid(stronghold, commodity);
        var crash = fair > 0
            && bestAsk > 0
            && (fair - bestAsk) * EconomyConstants.BasisPointsPer100Percent / fair
            >= MarketConstants.OpportunityCrashDiscountBp;
        var rally = fair > 0
            && bestBid > 0
            && (bestBid - fair) * EconomyConstants.BasisPointsPer100Percent / fair
            >= MarketConstants.OpportunityRallyPremiumBp;

        var hoard = 0;
        if (enemyCount > 0 || enemyNear || blockaded)
            hoard += MarketConstants.WarHoardMoneyShareShiftBp;
        if (enemyNear)
            hoard += MarketConstants.WarHoardMoneyShareShiftBp / 2;

        var dump = 0;
        if (harvestFear)
            dump += MarketConstants.HarvestDumpMoneyShareShiftBp;

        return new StrongholdMarketSignals(
            enemyCount,
            enemyNear,
            blockaded,
            harvestDay,
            harvestFear,
            crash,
            rally,
            hoard,
            dump);
    }

    private static bool HasNearbyHostileMilitary(Stronghold stronghold, GameData gameData)
    {
        foreach (var unit in gameData.Units.Values)
        {
            if (!unit.IsMilitary || unit.InStronghold || unit.ForceId == stronghold.ForceId)
                continue;
            if (!TransportRules.IsHostileForcePublic(stronghold.ForceId, unit.ForceId, gameData))
                continue;

            var dist = Math.Abs(unit.Location.X - stronghold.Location.X)
                       + Math.Abs(unit.Location.Y - stronghold.Location.Y);
            if (dist <= GarrisonBehaviorRules.ThreatManhattanDistance)
                return true;
        }

        return false;
    }

    private static bool IsWithinHarvestLeadWindow(
        Stronghold stronghold,
        GameDate date,
        IReadOnlyDictionary<int, RegionHarvestProfile> profiles,
        int regionId)
    {
        for (var offset = 1; offset <= MarketConstants.HarvestFearLeadDays; offset++)
        {
            var probe = AddDays(date, offset);
            if (HarvestRules.IsHarvestDay(stronghold, probe, profiles, regionId))
                return true;
        }

        return false;
    }

    private static GameDate AddDays(GameDate date, int days)
    {
        var day = date.Day + days;
        var month = date.Month;
        var year = date.Year;
        while (day > EconomyConstants.DaysPerMonth)
        {
            day -= EconomyConstants.DaysPerMonth;
            month++;
            if (month > 12)
            {
                month = 1;
                year++;
            }
        }

        return new GameDate(year, month, day);
    }
}
