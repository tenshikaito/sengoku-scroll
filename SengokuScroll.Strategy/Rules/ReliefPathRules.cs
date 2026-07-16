using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Domain.Services.Pathfinding;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>援防行军路径安全校验：途经格不得有敌对军事单位。</summary>
public static class ReliefPathRules
{
    /// <summary>除终点外，途经格上不得有敌对军事单位（援防不得穿越敌阵）。</summary>
    public static bool IsTransitPathClear(Unit unit, IReadOnlyList<PathNode> path, IGameWorldContext context)
    {
        if (path.Count < 2)
            return false;

        var gameData = context.GameWorld.GameData;
        if (!gameData.Forces.TryGetValue(unit.ForceId, out var myForce))
            return false;

        for (var i = 1; i < path.Count - 1; i++)
        {
            var location = path[i].Location;
            foreach (var occupant in context.GetUnitsAt(location))
            {
                if (!occupant.IsMilitary || occupant.Soldier <= 0 || occupant.Id == unit.Id)
                    continue;

                if (!gameData.Forces.TryGetValue(occupant.ForceId, out var otherForce))
                    continue;

                if (DiplomacyRules.IsEnemy(myForce, otherForce).IsSuccess
                    || WarRules.AreWarEnemies(unit.ForceId, occupant.ForceId, gameData))
                    return false;
            }
        }

        return true;
    }

    /// <summary>援防/支援行军是否适用途经敌阵禁令。</summary>
    public static bool RequiresClearTransit(Unit unit)
        => unit.Directive == UnitDirective.Support;
}
