using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Rules;

/// <summary>运输队软拦截与占格判定（M4-b）。</summary>
public static class TransportRules
{
    /// <summary>评估邻格与占格威胁等级（0=安全）。</summary>
    public static int EvaluateThreatLevel(SupplyConvoy convoy, GameData gameData)
    {
        // 移民队无所属势力，不受军事拦截威胁规则约束
        if (convoy.Purpose == TransportPurpose.Migrant && convoy.ForceId == 0)
            return 0;

        var threat = 0;
        var convoyForce = convoy.ForceId;

        foreach (var unit in gameData.Units.Values)
        {
            if (!unit.IsMilitary || unit.Soldier <= 0)
                continue;

            // 业务：己方部队不构成拦截威胁
            if (unit.ForceId == convoyForce)
                continue;

            // 业务：仅统计外交敌对势力的军事单位
            if (!IsHostileForce(convoyForce, unit.ForceId, gameData))
                continue;

            // 业务：敌军与运输队同格时威胁最高；若敌军正忙于其它战场交战则只挡不抢（降为邻格级威胁）
            if (unit.Location.X == convoy.Location.X && unit.Location.Y == convoy.Location.Y)
            {
                var busyInOtherBattle = unit.BattlefieldId > 0
                    && gameData.Battlefields.TryGetValue(unit.BattlefieldId, out var bf)
                    && !bf.IsClosed
                    && !(bf.Location.X == convoy.Location.X && bf.Location.Y == convoy.Location.Y);
                threat = Math.Max(
                    threat,
                    busyInOtherBattle
                        ? TransportConstants.AdjacentEnemyThreat
                        : TransportConstants.SameTileEnemyThreat);
                continue;
            }

            // 业务：敌军邻格时威胁次之
            if (IsAdjacent(convoy.Location, unit.Location))
                threat = Math.Max(threat, TransportConstants.AdjacentEnemyThreat);
        }

        // 业务：贸易运输队威胁下调（商队有护卫/隐蔽加成）
        if (convoy.Purpose == TransportPurpose.Trade)
            threat = threat * (EconomyConstants.BasisPointsPer100Percent - TransportConstants.TradePurposeThreatReductionBp)
                     / EconomyConstants.BasisPointsPer100Percent;

        // 业务：补给运输队威胁上调（军需车队更易被盯上）
        if (convoy.Purpose == TransportPurpose.Supply)
            threat = threat * TransportConstants.SupplyPurposeThreatIncreaseBp
                     / EconomyConstants.BasisPointsPer100Percent;

        return Math.Max(0, threat);
    }

    /// <summary>确定性拦截检定（0–99 roll &lt; threat 则触发）。</summary>
    public static bool ShouldIntercept(SupplyConvoy convoy, GameDate date, int threatLevel)
    {
        // 业务：威胁为 0 时不可能被拦截
        if (threatLevel <= 0)
            return false;

        var roll = DeterministicRoll(convoy.Id, date);
        // 业务：按当日确定性骰子与威胁值比对，上限 95% 拦截率
        return roll < Math.Min(95, threatLevel);
    }

    /// <summary>是否敌对势力（供贸易/外交规则复用）。</summary>
    public static bool IsHostileForcePublic(int a, int b, GameData gameData)
        => IsHostileForce(a, b, gameData);

    /// <summary>查找对运输队威胁最高的敌军单位 Id（用于缴获）。</summary>
    public static int? FindPrimaryThreatUnitId(SupplyConvoy convoy, GameData gameData)
    {
        if (convoy.Purpose == TransportPurpose.Migrant && convoy.ForceId == 0)
            return null;

        Unit? best = null;
        var bestThreat = 0;

        foreach (var unit in gameData.Units.Values)
        {
            if (!unit.IsMilitary || unit.Soldier <= 0)
                continue;

            if (!IsHostileForce(convoy.ForceId, unit.ForceId, gameData))
                continue;

            var threat = 0;
            if (unit.Location.X == convoy.Location.X && unit.Location.Y == convoy.Location.Y)
                threat = TransportConstants.SameTileEnemyThreat;
            else if (IsAdjacent(convoy.Location, unit.Location))
                threat = TransportConstants.AdjacentEnemyThreat;

            if (threat <= bestThreat)
                continue;

            bestThreat = threat;
            best = unit;
        }

        return best?.Id;
    }

    /// <summary>判定两势力是否处于敌对状态（任一方声明 Enemy 即敌对）。</summary>
    private static bool IsHostileForce(int a, int b, GameData gameData)
    {
        // 业务：势力数据缺失时保守视为敌对，避免误放行
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

    /// <summary>按运输队 Id 与日期生成 0–99 确定性骰子（同日同队结果可复现）。</summary>
    private static int DeterministicRoll(int convoyId, GameDate date)
        => Math.Abs(HashCode.Combine(convoyId, date.Year, date.Month, date.Day)) % 100;

    /// <summary>曼哈顿距离为 1 的上下左右邻格。</summary>
    private static bool IsAdjacent(Point3 a, Point3 b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1;
}
