using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Actions;

/// <summary>从 Home 据点 SubUnit 池组建 Unit（默认在城中）。</summary>
public static class UnitComposeActions
{
    public static GameResult<Unit> ComposeFromStronghold(
        IGameWorldContext context,
        Stronghold stronghold,
        StrategyScenarioMeta? meta,
        GameData gameData,
        int playerForceId,
        string unitName,
        int commanderId,
        IReadOnlyList<StrategyDeployCompositionEntry> composition,
        bool deployToMap = false,
        int? food = null,
        int? money = null,
        bool requireLordResidence = true)
    {
        if (meta is null)
            return GameError.DataNotFound;

        var validate = UnitComposeRules.ValidateCompose(
            stronghold,
            meta,
            gameData,
            playerForceId,
            commanderId,
            composition,
            deployToMap,
            requireLordResidence);
        if (!validate.IsSuccess)
            return validate.Error!;

        var totalSoldiers = composition.Sum(c => c.Soldiers);
        var unitId = gameData.Units.Keys.Where(id => id > 0).DefaultIfEmpty(100).Max() + 1;

        var unit = new Unit
        {
            Id = unitId,
            Name = string.IsNullOrWhiteSpace(unitName) ? $"{stronghold.Name}队" : unitName.Trim(),
            ForceId = playerForceId,
            Location = stronghold.Location,
            Soldier = totalSoldiers,
            Food = food ?? Math.Min(stronghold.ForceActor.Food / 8, totalSoldiers * 2000),
            Money = money ?? Math.Min(stronghold.ForceActor.Money / 10, totalSoldiers * 500),
            Morale = stronghold.ForceActor.Morale,
            Training = stronghold.ForceActor.Training,
            Movement = 5,
            Ap = 5,
            IsMilitary = true,
            Kind = UnitKind.Military,
            InStronghold = !deployToMap,
            HomeStrongholdId = stronghold.Id,
            LocationStrongholdId = stronghold.Id,
            Directive = UnitDirective.Move,
            Stance = UnitStance.Normal,
            Status = UnitStatus.Waiting,
            LeaderId = commanderId,
            SubUnitIds = [],
            ActionTarget = new UnitActionTarget
            {
                ForceId = playerForceId,
                StrongholdId = stronghold.Id,
                RoutePoints = new Queue<Point2>()
            }
        };

        var allocated = new List<SubUnit>();
        foreach (var entry in composition)
        {
            if (entry.Soldiers <= 0)
                continue;

            if (!StrongholdMilitaryBootstrapHelper.TryAllocateGarrisonTroops(
                    stronghold,
                    gameData,
                    entry.TypeId,
                    entry.Soldiers,
                    unitId,
                    allocated))
            {
                StrongholdMilitaryBootstrapHelper.ReturnSubUnitsToGarrison(stronghold, gameData, allocated);
                return GameError.StrongholdError.InsufficientGarrisonTroops;
            }
        }

        foreach (var sub in allocated)
            unit.SubUnitIds.Add(sub.Id);

        if (commanderId > 0
            && gameData.Characters.TryGetValue(commanderId, out var commander))
        {
            UnitCommanderHelper.AttachToUnit(commander, unit);
        }

        gameData.Units[unitId] = unit;
        if (deployToMap)
            MapLocationActions.RegisterUnit(context.GameWorld, unit);

        StrongholdMilitaryStatsHelper.Recalculate(stronghold, gameData);
        return unit;
    }
}
