using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Character;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Actions;

/// <summary>Unit 入城、出城、建制解散与强制解散。</summary>
public static class UnitStrongholdPresenceActions
{
    public static GameResult EnterStronghold(
        IGameWorldContext context,
        Unit unit,
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta? meta = null)
    {
        if (!UnitStrongholdPresenceRules.CanEnterStronghold(unit, stronghold, gameData))
            return GameError.MovementError.CannotMoveToTile;

        if (!unit.InStronghold)
            MapLocationActions.UnregisterUnitFromMap(context, unit);

        unit.InStronghold = true;
        unit.LocationStrongholdId = stronghold.Id;
        unit.Location = stronghold.Location;
        unit.ActionTarget.RoutePoints.Clear();
        unit.SiegeMode = UnitSiegeMode.None;
        unit.Stance = UnitStance.Normal;
        unit.Status = UnitStatus.Waiting;

        if (unit.HomeStrongholdId <= 0)
            unit.HomeStrongholdId = stronghold.Id;

        if (meta is not null)
            ReserveCommanderRules.TryUpgradeReserveCommander(unit, stronghold, gameData, meta);

        SyncCommanderLocation(unit, gameData);
        return GameResult.Ok();
    }

    public static GameResult ExitStronghold(
        IGameWorldContext context,
        Unit unit,
        Stronghold stronghold,
        GameData gameData)
    {
        if (!UnitStrongholdPresenceRules.CanExitStronghold(unit, stronghold))
            return GameError.MovementError.CannotMoveToTile;

        unit.InStronghold = false;
        unit.Location = stronghold.Location;
        MapLocationActions.RegisterUnitOnMap(context, unit);
        SyncCommanderLocation(unit, gameData);
        return GameResult.Ok();
    }

    /// <summary>建制解散：仅 Home 据点；SubUnit 回列表，物资归城。</summary>
    public static GameResult OrganizationalDisband(
        IGameWorldContext context,
        Unit unit,
        GameData gameData)
    {
        if (!UnitStrongholdPresenceRules.CanOrganizationalDisband(unit, gameData))
            return GameError.DataNotFound;

        if (!gameData.Strongholds.TryGetValue(unit.HomeStrongholdId, out var home))
            return GameError.StrongholdError.StrongholdNotFound;

        ReturnUnitAssetsToStronghold(unit, home);
        ReturnSubUnitsToHome(unit, home, gameData);
        DetachCommanders(unit, gameData);
        MapLocationActions.RemoveUnit(context, unit);
        StrongholdMilitaryStatsHelper.Recalculate(home, gameData);
        return GameResult.Ok();
    }

    /// <summary>强制解散：任意地点；建制崩溃，SubUnit 不 recover；信使回报本家。</summary>
    public static GameResult ForcedDisband(
        IGameWorldContext context,
        Unit unit,
        GameData gameData,
        StrategyScenarioMeta meta,
        StrategyDayOutcomeBuffer? dayOutcomeBuffer,
        BattleReportDeliveryHelper? reportDelivery = null)
    {
        var origin = ResolveReportOriginStronghold(unit, gameData);
        var commanderNames = CollectCommanderNames(unit, gameData);
        var brief = $"💥 {unit.Name} 建制崩溃";
        var detail = commanderNames.Count > 0
            ? $"{unit.Name} 因粮尽士气归零而溃散；{string.Join("、", commanderNames)} 下落不明。"
            : $"{unit.Name} 因粮尽士气归零而溃散。";

        DetachCommanders(unit, gameData);
        MapLocationActions.RemoveUnit(context, unit);

        var reportEvent = new StrategyEventDto
        {
            Category = "UnitForcedDisband",
            Brief = brief,
            Message = detail,
            DetailCategory = "UnitForcedDisband",
            DetailMessage = detail
        };

        dayOutcomeBuffer?.AddEvent(reportEvent);

        if (reportDelivery is not null && unit.ForceId == meta.PlayerForceId)
        {
            reportDelivery.DeliverPlayerStrategicReport(
                meta.PlayerForceId,
                origin?.Location ?? unit.Location,
                gameData,
                reportEvent);
        }

        return GameResult.Ok();
    }

    private static void ReturnUnitAssetsToStronghold(Unit unit, Stronghold home)
    {
        if (unit.Food > 0)
        {
            home.ForceActor.Food += unit.Food;
            unit.Food = 0;
        }

        if (unit.Money > 0)
        {
            home.ForceActor.Money += unit.Money;
            unit.Money = 0;
        }
    }

    private static void ReturnSubUnitsToHome(Unit unit, Stronghold home, GameData gameData)
    {
        var subs = unit.SubUnitIds
            .Where(id => gameData.SubUnits.TryGetValue(id, out _))
            .Select(id => gameData.SubUnits[id])
            .ToList();

        StrongholdMilitaryBootstrapHelper.ReturnSubUnitsToGarrison(home, gameData, subs);
        unit.SubUnitIds.Clear();
    }

    private static void DetachCommanders(Unit unit, GameData gameData)
    {
        foreach (var commanderId in UnitCommanderEscapeHelper.CollectCommanderIds(unit, gameData))
        {
            if (!gameData.Characters.TryGetValue(commanderId, out var commander))
                continue;

            UnitCommanderHelper.DetachFromStronghold(commander, unit.LocationStrongholdId > 0
                ? unit.LocationStrongholdId
                : unit.HomeStrongholdId);
        }

        unit.LeaderId = 0;
    }

    private static void SyncCommanderLocation(Unit unit, GameData gameData)
    {
        foreach (var commanderId in UnitCommanderEscapeHelper.CollectCommanderIds(unit, gameData))
        {
            if (!gameData.Characters.TryGetValue(commanderId, out var commander))
                continue;

            commander.Location = unit.Location;
        }
    }

    private static Stronghold? ResolveReportOriginStronghold(Unit unit, GameData gameData)
    {
        if (unit.HomeStrongholdId > 0
            && gameData.Strongholds.TryGetValue(unit.HomeStrongholdId, out var home))
        {
            return home;
        }

        if (unit.LocationStrongholdId > 0
            && gameData.Strongholds.TryGetValue(unit.LocationStrongholdId, out var current))
        {
            return current;
        }

        return gameData.Strongholds.Values.FirstOrDefault(s => s.ForceId == unit.ForceId);
    }

    private static IReadOnlyList<string> CollectCommanderNames(Unit unit, GameData gameData)
    {
        var names = new List<string>();
        foreach (var commanderId in UnitCommanderEscapeHelper.CollectCommanderIds(unit, gameData))
        {
            if (gameData.Characters.TryGetValue(commanderId, out var commander) && !commander.IsDead)
                names.Add(commander.Name);
        }

        if (names.Count == 0 && ReserveCommanderRules.IsReserveCommander(unit))
            names.Add(ReserveCommanderRules.ReserveCommanderDisplayName);

        return names;
    }
}
