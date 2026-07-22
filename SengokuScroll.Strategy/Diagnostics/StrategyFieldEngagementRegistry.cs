using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>相邻野战对峙累计日数（非持久化；仿真重启后清零）。</summary>
public sealed class StrategyFieldEngagementRegistry
{
    private readonly Dictionary<(int A, int B), int> standoffDays = [];

    public static (int A, int B) PairKey(int unitAId, int unitBId)
        => unitAId < unitBId ? (unitAId, unitBId) : (unitBId, unitAId);

    public bool IsLargeArmyEngagement(Unit unitA, Unit unitB)
        => unitA.Soldier + unitB.Soldier >= BattleConstants.LargeArmySoldierThreshold;

    public int GetStandoffDays(int unitAId, int unitBId)
    {
        var key = PairKey(unitAId, unitBId);
        return standoffDays.TryGetValue(key, out var days) ? days : 0;
    }

    public void SetStandoffDays(int unitAId, int unitBId, int days)
        => standoffDays[PairKey(unitAId, unitBId)] = days;

    public void ClearStandoff(int unitAId, int unitBId)
        => standoffDays.Remove(PairKey(unitAId, unitBId));

    public void PruneNonAdjacent(GameData gameData)
    {
        foreach (var key in standoffDays.Keys.ToList())
        {
            var hasA = gameData.Units.TryGetValue(key.A, out var a);
            var hasB = gameData.Units.TryGetValue(key.B, out var b);
            // 业务：邻格对峙已废；仅同格（或同 Battlefield）保持登记
            var inRange = hasA && hasB && MoveEngagementRules.IsInEngagementRange(a!, b!);

            if (!inRange)
            {
                standoffDays.Remove(key);
                if (hasA)
                    BattlefieldEngagementRules.LeaveBattlefield(a!);
                if (hasB)
                    BattlefieldEngagementRules.LeaveBattlefield(b!);
            }
        }

        // 业务：登记表外仍残留 Standoff/Attacking 锁定时，对手已离场则强制脱离
        PruneOrphanEngagements(gameData);

        // 业务：同步 Battlefield 对峙日
        foreach (var bf in gameData.Battlefields.Values.Where(b => !b.IsClosed))
        {
            if (bf.MainCombatantAUnitId > 0 && bf.MainCombatantBUnitId > 0)
                SetStandoffDays(bf.MainCombatantAUnitId, bf.MainCombatantBUnitId, bf.StandoffDays);
        }
    }

    /// <summary>清除对手无效或已不同格的接敌锁定（避免 AI 永久 Skip）。</summary>
    public void PruneOrphanEngagements(GameData gameData)
    {
        foreach (var unit in gameData.Units.Values.ToList())
        {
            if (unit.ActionTarget.UnitId <= 0)
                continue;

            if (unit.Status != UnitStatus.Standoff
                && unit.Stance != UnitStance.Attacking)
                continue;

            var opponentId = unit.ActionTarget.UnitId;
            if (!gameData.Units.TryGetValue(opponentId, out var opponent)
                || opponent.Soldier <= 0
                || !MoveEngagementRules.IsInEngagementRange(unit, opponent))
            {
                ClearStandoff(unit.Id, opponentId);
                BattlefieldEngagementRules.LeaveBattlefield(unit);
            }
        }
    }
}
