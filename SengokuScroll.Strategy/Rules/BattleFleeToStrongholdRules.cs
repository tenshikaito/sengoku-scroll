using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Models;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>
/// 战局不利时逃入友城：邻格/同格友方据点，兵力吸入城内并移除野战单位。
/// </summary>
public static class BattleFleeToStrongholdRules
{
    /// <summary>败方败退后尝试逃入最近友城；成功则返回据点。</summary>
    public static Stronghold? TryFleeAfterDefeat(
        IGameWorldContext worldContext,
        Unit loser,
        Unit winner,
        GameData gameData,
        StrategyDayOutcomeBuffer? dayOutcomeBuffer)
    {
        if (loser.Soldier <= 0)
            return null;

        // 业务：已在城格或邻格支援的友军战败时，残部并入城内（敌占城格时亦可挤入）
        if (loser.Directive == UnitDirective.Support)
        {
            var refuge = FindNearestRefugeStronghold(loser, gameData);
            if (refuge is not null)
            {
                var soldiers = loser.Soldier;
                var name = loser.Name;

                StrongholdGarrisonRules.AbsorbSoldiersIntoCity(refuge, soldiers);
                if (loser.Location.IsSameTile(refuge.Location))
                    StrongholdGarrisonRules.SyncCityMoraleFromUnit(refuge, loser);

                MapLocationActions.RemoveUnit(worldContext, loser);
                gameData.Units.Remove(loser.Id);

                dayOutcomeBuffer?.AddEvent(new StrategyEventDto
                {
                    Category = "UnitFledToStronghold",
                    Brief = $"🏯 {name} 撤回 {refuge.Name}",
                    Message =
                        $"{name} 战败后 {soldiers} 人撤回 {refuge.Name} 城内" +
                        (GarrisonBehaviorRules.IsEnemyOccupyingStrongholdTile(refuge, gameData, out _)
                            ? "（城格仍有敌军）"
                            : "") +
                        "。"
                });

                if (winner.ActionTarget.UnitId == loser.Id)
                {
                    winner.ActionTarget.UnitId = 0;
                    if (winner.Stance == UnitStance.Attacking)
                        winner.Stance = UnitStance.Normal;
                }

                return refuge;
            }
        }

        if (loser.SiegeMode != UnitSiegeMode.None)
            return null;

        var refugeFallback = FindNearestRefugeStronghold(loser, gameData);
        if (refugeFallback is null)
            return null;

        // 业务：邻格友城可收容败军；城格有敌时仍允许残部挤入城内
        var soldiersFallback = loser.Soldier;
        var nameFallback = loser.Name;
        var from = loser.Location;

        StrongholdGarrisonRules.AbsorbSoldiersIntoCity(refugeFallback, soldiersFallback);
        MapLocationActions.RemoveUnit(worldContext, loser);
        gameData.Units.Remove(loser.Id);

        dayOutcomeBuffer?.AddEvent(new StrategyEventDto
        {
            Category = "UnitFledToStronghold",
            Brief = $"🏯 {nameFallback} 逃入 {refugeFallback.Name}",
            Message =
                $"{nameFallback} 战败后自 ({from.X},{from.Y}) 逃入 {refugeFallback.Name}，" +
                $"残部 {soldiersFallback} 人并入城内守备。"
        });

        // 业务：胜方失去追击目标时清除攻击令
        if (winner.ActionTarget.UnitId == loser.Id)
        {
            winner.ActionTarget.UnitId = 0;
            if (winner.Stance == UnitStance.Attacking)
                winner.Stance = UnitStance.Normal;
        }

        return refugeFallback;
    }

    /// <summary>战术中余兵不足时是否具备逃城条件（邻友城且城格未被敌占）。</summary>
    public static bool CanFleeDuringBattle(Unit unit, GameData gameData)
    {
        if (unit.Soldier <= 0)
            return false;

        var refuge = FindNearestRefugeStronghold(unit, gameData);
        return refuge is not null;
    }

    private static Stronghold? FindNearestRefugeStronghold(Unit unit, GameData gameData)
    {
        Stronghold? best = null;
        var bestDist = int.MaxValue;

        foreach (var stronghold in gameData.Strongholds.Values)
        {
            if (stronghold.ForceId != unit.ForceId)
                continue;

            var same = unit.Location.IsSameTile(stronghold.Location);
            var adjacent = unit.Location.IsAdjacent(stronghold.Location);
            if (!same && !adjacent)
                continue;

            var dist = same ? 0 : 1;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = stronghold;
            }
        }

        return best;
    }
}
