using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Helpers;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Actions;

/// <summary>从现有部队拆出子编制，在邻格生成新部队。</summary>
public static class UnitSplitActions
{
    public static GameResult<Unit> SplitSubUnits(
        IGameWorldContext context,
        Unit parent,
        IReadOnlyList<int> subUnitIds,
        Point3 spawnLocation,
        GameData gameData,
        string? unitName = null)
    {
        var validate = UnitSplitRules.ValidateSplit(parent, subUnitIds, spawnLocation, gameData);
        if (!validate.IsSuccess)
            return validate.Error!;

        var newUnitId = gameData.Units.Keys.Where(id => id > 0).DefaultIfEmpty(100).Max() + 1;
        var splitSubs = subUnitIds
            .Where(id => gameData.SubUnits.ContainsKey(id))
            .ToList();

        var newUnit = new Unit
        {
            Id = newUnitId,
            Name = string.IsNullOrWhiteSpace(unitName) ? $"{parent.Name}分遣" : unitName.Trim(),
            ForceId = parent.ForceId,
            Location = spawnLocation,
            Morale = parent.Morale,
            Training = parent.Training,
            Movement = parent.Movement,
            Ap = parent.Movement,
            Food = 0,
            Money = 0,
            IsMilitary = true,
            Directive = UnitDirective.Move,
            Stance = UnitStance.Normal,
            Status = UnitStatus.Waiting,
            SubUnitIds = [],
            ActionTarget = new UnitActionTarget { RoutePoints = new Queue<Point2>() }
        };

        foreach (var subId in splitSubs)
        {
            if (!gameData.SubUnits.TryGetValue(subId, out var sub))
                continue;

            parent.SubUnitIds.Remove(subId);
            sub.UnitId = newUnitId;
            newUnit.SubUnitIds.Add(subId);

            if (sub.LeaderId > 0 && newUnit.LeaderId <= 0
                && gameData.Characters.TryGetValue(sub.LeaderId, out var subCommander))
            {
                newUnit.LeaderId = subCommander.Id;
                UnitCommanderHelper.AttachToUnit(subCommander, newUnit);
            }
        }

        newUnit.Soldier = newUnit.SubUnitIds.Sum(id =>
            gameData.SubUnits.TryGetValue(id, out var s) ? s.Soldier : 0);
        parent.Soldier = parent.SubUnitIds.Count > 0
            ? parent.SubUnitIds.Sum(id => gameData.SubUnits.TryGetValue(id, out var s) ? s.Soldier : 0)
            : Math.Max(0, parent.Soldier - newUnit.Soldier);

        gameData.Units[newUnitId] = newUnit;
        MapLocationActions.RegisterUnit(context.GameWorld, newUnit);

        return newUnit;
    }
}
