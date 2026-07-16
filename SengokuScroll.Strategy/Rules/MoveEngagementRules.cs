using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Battle;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>同格接敌判定（邻格开战已废止）。</summary>
public static class MoveEngagementRules
{
    /// <summary>会主动接敌的方针。</summary>
    public static bool IsAggressiveDirective(UnitDirective directive)
        => directive is UnitDirective.Occupy or UnitDirective.Raid;

    /// <summary>两军同格且敌对、至少一方进攻（或攻城）时应接敌。</summary>
    public static bool ShouldEngage(Unit unitA, Unit unitB, Force? forceA, Force? forceB, GameData gameData)
    {
        if (unitA.Soldier <= 0 || unitB.Soldier <= 0)
            return false;

        if (unitA.Status is UnitStatus.Chaos or UnitStatus.Routing
            || unitB.Status is UnitStatus.Chaos or UnitStatus.Routing)
            return false;

        // 业务：仅同格开战（无邻格对峙）
        if (!unitA.Location.IsSameTile(unitB.Location))
            return false;

        var hostile = (forceA is not null && forceB is not null && DiplomacyRules.IsEnemy(forceA, forceB).IsSuccess)
                      || WarRules.AreWarEnemies(unitA.ForceId, unitB.ForceId, gameData);

        if (!hostile)
            return false;

        // 业务：已在同一战场则由对峙/结算维护，不重复排队
        if (unitA.BattlefieldId > 0 && unitA.BattlefieldId == unitB.BattlefieldId)
            return unitA.ActionTarget.UnitId == 0 || unitB.ActionTarget.UnitId == 0;

        var aAgg = IsAggressiveDirective(unitA.Directive) && unitA.SiegeMode != UnitSiegeMode.Encircle;
        var bAgg = IsAggressiveDirective(unitB.Directive) && unitB.SiegeMode != UnitSiegeMode.Encircle;
        var siege = TryGetSiegeAggressor(unitA, unitB, gameData) is not null;
        var eitherSiegeOrder = unitA.SiegeMode != UnitSiegeMode.None || unitB.SiegeMode != UnitSiegeMode.None;

        // 业务：同格敌对即应开战；进攻方针或攻城令优先触发
        if (!aAgg && !bAgg && !siege && !eitherSiegeOrder)
        {
            // 业务：相遇即自动进入战场（无进攻方针时也建容器）
            return true;
        }

        if (TryGetSiegeAggressor(unitA, unitB, gameData) is { } siegeAggressor)
        {
            if (!BattleFactorEvaluator.CanUnitEngage(siegeAggressor))
                return false;

            return true;
        }

        if (!BattleFactorEvaluator.CanUnitEngage(unitA) || !BattleFactorEvaluator.CanUnitEngage(unitB))
            return false;

        return true;
    }

    private static Unit? TryGetSiegeAggressor(Unit unitA, Unit unitB, GameData gameData)
    {
        if (IsAggressiveDirective(unitA.Directive)
            && SiegeBattleRules.IsSiegeEngagement(unitA, unitB, gameData))
            return unitA;

        if (IsAggressiveDirective(unitB.Directive)
            && SiegeBattleRules.IsSiegeEngagement(unitB, unitA, gameData))
            return unitB;

        return null;
    }

    /// <summary>判定本回合应由哪方发起接敌（单方进攻方针时）。双方皆进攻时返回 null，交由互攻规则。</summary>
    public static Unit? ResolveSingleAggressor(Unit unitA, Unit unitB)
    {
        var aAgg = IsAggressiveDirective(unitA.Directive);
        var bAgg = IsAggressiveDirective(unitB.Directive);

        if (aAgg && !bAgg)
            return unitA;

        if (bAgg && !aAgg)
            return unitB;

        return null;
    }

    /// <summary>仅同格可接敌（邻格开战已废）。</summary>
    public static bool IsInEngagementRange(Unit unitA, Unit unitB)
        => unitA.Location.IsSameTile(unitB.Location);
}
