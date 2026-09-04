using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Helpers;

namespace SengokuScroll.Strategy.Rules;

/// <summary>运输 Unit 软拦截与占格判定（M4-b）。</summary>
public static class TransportRules
{
    /// <summary>评估邻格与占格威胁等级（0=安全）。</summary>
    public static int EvaluateThreatLevel(Unit transport, GameData gameData)
    {
        if (transport.TransportPurpose == TransportPurpose.Migrant && transport.ForceId == 0)
            return 0;

        var threat = 0;
        var convoyForce = transport.ForceId;

        foreach (var unit in gameData.Units.Values)
        {
            if (!unit.IsMilitary || unit.Soldier <= 0)
                continue;

            if (unit.ForceId == convoyForce)
                continue;

            if (!IsHostileForce(convoyForce, unit.ForceId, gameData))
                continue;

            if (unit.Location.X == transport.Location.X && unit.Location.Y == transport.Location.Y)
            {
                var busyInOtherBattle = unit.BattlefieldId > 0
                    && gameData.Battlefields.TryGetValue(unit.BattlefieldId, out var bf)
                    && !bf.IsClosed
                    && !(bf.Location.X == transport.Location.X && bf.Location.Y == transport.Location.Y);
                threat = Math.Max(
                    threat,
                    busyInOtherBattle
                        ? TransportConstants.AdjacentEnemyThreat
                        : TransportConstants.SameTileEnemyThreat);
                continue;
            }

            if (IsAdjacent(transport.Location, unit.Location))
                threat = Math.Max(threat, TransportConstants.AdjacentEnemyThreat);
        }

        if (transport.TransportPurpose == TransportPurpose.Trade)
            threat = threat * (EconomyConstants.BasisPointsPer100Percent - TransportConstants.TradePurposeThreatReductionBp)
                     / EconomyConstants.BasisPointsPer100Percent;

        if (transport.TransportPurpose == TransportPurpose.Supply)
            threat = threat * TransportConstants.SupplyPurposeThreatIncreaseBp
                     / EconomyConstants.BasisPointsPer100Percent;

        return Math.Max(0, threat);
    }

    /// <summary>确定性拦截检定（0–99 roll &lt; threat 则触发）。</summary>
    public static bool ShouldIntercept(Unit transport, GameDate date, int threatLevel)
    {
        if (threatLevel <= 0)
            return false;

        var roll = DeterministicRoll(transport.Id, date);
        return roll < Math.Min(95, threatLevel);
    }

    /// <summary>是否敌对势力（供贸易/外交规则复用）。</summary>
    public static bool IsHostileForcePublic(int a, int b, GameData gameData)
        => IsHostileForce(a, b, gameData);

    /// <summary>查找对运输 Unit 威胁最高的敌军单位 Id（用于缴获）。</summary>
    public static int? FindPrimaryThreatUnitId(Unit transport, GameData gameData)
    {
        if (transport.TransportPurpose == TransportPurpose.Migrant && transport.ForceId == 0)
            return null;

        Unit? best = null;
        var bestThreat = 0;

        foreach (var unit in gameData.Units.Values)
        {
            if (!unit.IsMilitary || unit.Soldier <= 0)
                continue;

            if (!IsHostileForce(transport.ForceId, unit.ForceId, gameData))
                continue;

            var threat = 0;
            if (unit.Location.X == transport.Location.X && unit.Location.Y == transport.Location.Y)
                threat = TransportConstants.SameTileEnemyThreat;
            else if (IsAdjacent(transport.Location, unit.Location))
                threat = TransportConstants.AdjacentEnemyThreat;

            if (threat <= bestThreat)
                continue;

            bestThreat = threat;
            best = unit;
        }

        return best?.Id;
    }

    private static bool IsHostileForce(int a, int b, GameData gameData)
    {
        if (!gameData.Forces.TryGetValue(a, out var forceA) || !gameData.Forces.TryGetValue(b, out var forceB))
            return true;

        if (forceA.ForceId == forceB.ForceId)
            return false;

        foreach (var dip in forceA.Diplomacies)
        {
            if (dip.TargetForceId == b && dip.Relation == Diplomacy.DiplomacyRelation.Enemy)
                return true;
        }

        foreach (var dip in forceB.Diplomacies)
        {
            if (dip.TargetForceId == a && dip.Relation == Diplomacy.DiplomacyRelation.Enemy)
                return true;
        }

        return false;
    }

    private static int DeterministicRoll(int convoyId, GameDate date)
        => DeterministicHash.Combine(convoyId, date.Year, date.Month, date.Day) % 100;

    private static bool IsAdjacent(Point3 a, Point3 b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1;
}
