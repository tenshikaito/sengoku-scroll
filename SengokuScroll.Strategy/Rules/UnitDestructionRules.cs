using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using static SengokuScroll.Domain.Entities.Character;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>部队溃灭：移除地图单位，处理将领命运与战利品。</summary>
public static class UnitDestructionRules
{
    /// <summary>击溃敌军时可缴获的粮草比例（万分比，4500 即 45%）。</summary>
    public const int LootFoodBasisPoints = 4500;

    /// <summary>击溃敌军时可缴获的金钱比例（万分比，3500 即 35%）。</summary>
    public const int LootMoneyBasisPoints = 3500;

    /// <summary>溃灭处理结果：单位是否移除、主将命运、缴获粮金数量。</summary>
    public sealed record DestructionOutcome(
        bool UnitRemoved,
        string? CommanderFate,
        int LootedFood,
        int LootedMoney);

    /// <summary>日终清扫兵数为 0 的野战部队，逐一触发溃灭流程。</summary>
    public static void PurgeZeroSoldierUnits(
        IGameWorldContext context,
        GameData gameData,
        StrategyScenarioMeta meta,
        StrategyDayOutcomeBuffer? dayOutcomeBuffer)
    {
        // 业务：先收集 Id 再处理，避免遍历中移除单位导致集合修改异常
        foreach (var unit in gameData.Units.Values
                     .Where(u => u.IsMilitary && u.Soldier <= 0)
                     .Select(u => u.Id)
                     .ToList())
        {
            if (!gameData.Units.TryGetValue(unit, out var doomed))
                continue;

            ResolveAnnihilatedUnit(context, doomed, null, gameData, meta, dayOutcomeBuffer, pathfinding: null);
        }
    }

    /// <summary>处理一支已溃灭部队：驻军联动、战利品、主将命运、移出地图与事件记录。</summary>
    public static DestructionOutcome ResolveAnnihilatedUnit(
        IGameWorldContext context,
        Unit destroyed,
        Unit? victor,
        GameData gameData,
        StrategyScenarioMeta meta,
        StrategyDayOutcomeBuffer? dayOutcomeBuffer,
        BattleReportDeliveryHelper? reportDelivery = null,
        IPathfindingService? pathfinding = null)
    {
        if (destroyed.Soldier > 0)
            return new DestructionOutcome(false, null, 0, 0);

        StrongholdGarrisonActions.OnGarrisonUnitDestroyed(destroyed, gameData);

        var lootFood = 0;
        var lootMoney = 0;
        // 业务：有胜方且仍成军时，按万分比掠夺溃灭部队余粮余金
        if (victor is not null && victor.Soldier > 0)
        {
            lootFood = destroyed.Food * LootFoodBasisPoints / 10000;
            lootMoney = destroyed.Money * LootMoneyBasisPoints / 10000;
            if (lootFood > 0)
                victor.Food += lootFood;
            if (lootMoney > 0)
                victor.Money += lootMoney;
        }

        var releaseLocation = destroyed.Location;
        var commanderFate = ResolveCommandersFate(
            context,
            destroyed,
            victor,
            releaseLocation,
            gameData,
            meta,
            dayOutcomeBuffer,
            pathfinding);

        MapLocationActions.RemoveUnit(context, destroyed);

        var brief = commanderFate switch
        {
            "Captured" => $"⛓ {destroyed.Name} 溃灭，主将被俘",
            "Escaped" => $"🏃 {destroyed.Name} 溃灭，主将逃脱",
            "Slain" => $"💀 {destroyed.Name} 溃灭，主将阵亡",
            _ => $"💥 {destroyed.Name} 溃灭"
        };

        var victorName = victor is { Soldier: > 0 } ? victor.Name : "敌军";
        var lootPart = lootFood + lootMoney > 0
            ? $"，缴获粮 {lootFood} 合、金 {lootMoney} 文"
            : string.Empty;

        var reportEvent = new StrategyEventDto
        {
            Category = "UnitDestroyed",
            Brief = brief,
            Message = $"{victorName} 击溃 {destroyed.Name}{lootPart}。{FormatCommanderMessage(destroyed, commanderFate)}"
        };

        if (reportDelivery is not null
            && (destroyed.ForceId == meta.PlayerForceId || victor?.ForceId == meta.PlayerForceId))
        {
            reportDelivery.DeliverPlayerStrategicReport(
                meta.PlayerForceId,
                destroyed.Location,
                gameData,
                reportEvent);
        }

        return new DestructionOutcome(true, commanderFate, lootFood, lootMoney);
    }

    private static string? ResolveCommandersFate(
        IGameWorldContext context,
        Unit destroyed,
        Unit? victor,
        Point3 releaseLocation,
        GameData gameData,
        StrategyScenarioMeta meta,
        StrategyDayOutcomeBuffer? dayOutcomeBuffer,
        IPathfindingService? pathfinding)
    {
        var commanderIds = UnitCommanderEscapeHelper.CollectCommanderIds(destroyed, gameData).ToList();
        if (commanderIds.Count == 0)
            return null;

        string? mainFate = null;
        var mainCommanderId = destroyed.LeaderId;

        foreach (var commanderId in commanderIds)
        {
            if (!gameData.Characters.TryGetValue(commanderId, out var commander)
                || commander.IsDead
                || commander.ForceStatus == CharacterForceStatus.Prisoner)
            {
                continue;
            }

            var fate = ResolveSingleCommanderFate(
                context,
                destroyed,
                commander,
                victor,
                releaseLocation,
                gameData,
                meta,
                pathfinding);

            if (commanderId == mainCommanderId)
                mainFate = fate;
        }

        return mainFate;
    }

    private static string ResolveSingleCommanderFate(
        IGameWorldContext context,
        Unit destroyed,
        Character commander,
        Unit? victor,
        Point3 releaseLocation,
        GameData gameData,
        StrategyScenarioMeta meta,
        IPathfindingService? pathfinding)
    {
        // 业务：无胜方（如补给断绝等非战斗溃灭）时，将领在溃灭格现身并寻路回居城
        if (victor is null || victor.Soldier <= 0)
        {
            EscapeCommanderToMap(context, commander, releaseLocation, gameData, meta, pathfinding);
            return "Escaped";
        }

        gameData.Characters.TryGetValue(victor.LeaderId, out var victorCommander);
        var aggressiveness = victorCommander?.Personality.Action ?? 55;
        // 业务：基础逃脱率 25%；统率/武勇提高、胜方行动性降低主将逃脱概率
        var escapeChance = 25;
        escapeChance += commander.Leadership / 5;
        escapeChance += commander.Power / 8;
        escapeChance -= aggressiveness / 4;

        // 业务：胜方兵力超过溃军两倍时，主将更难脱身
        if (victor.Soldier > destroyed.Soldier * 2)
            escapeChance -= 15;

        var roll = Math.Abs(HashCode.Combine(
            gameData.SimulationSeed,
            destroyed.Id,
            victor.Id,
            commander.Id,
            gameData.GameDate.Year,
            gameData.GameDate.Month,
            gameData.GameDate.Day)) % 100;

        // 业务：掷点低于逃脱率则逃回；介于逃脱率与 +35 之间则被俘；否则战死
        if (roll < escapeChance)
        {
            EscapeCommanderToMap(context, commander, releaseLocation, gameData, meta, pathfinding);
            return "Escaped";
        }

        if (roll < escapeChance + 35)
        {
            CaptureCommander(commander, victor, gameData);
            return "Captured";
        }

        commander.IsDead = true;
        commander.ForceStatus = CharacterForceStatus.Idle;
        return "Slain";
    }

    private static void EscapeCommanderToMap(
        IGameWorldContext context,
        Character commander,
        Point3 releaseLocation,
        GameData gameData,
        StrategyScenarioMeta meta,
        IPathfindingService? pathfinding)
    {
        if (pathfinding is not null)
        {
            UnitCommanderEscapeHelper.ReleaseToMapAndRouteHome(
                context,
                commander,
                releaseLocation,
                gameData,
                meta,
                pathfinding);
            return;
        }

        // 业务：无寻路服务时（如日终清扫）仍避免瞬移，至少落在溃灭格
        commander.ForceStatus = CharacterForceStatus.Idle;
        commander.LocationType = CharacterLocationType.Map;
        commander.LocationStrongholdId = 0;
        commander.ActionStatus = CharacterActionStatus.Waiting;
        commander.ActionTarget.RoutePoints.Clear();
        MapLocationActions.SetCharacterLocation(context, commander, releaseLocation);
    }

    private static void CaptureCommander(Character commander, Unit victor, GameData gameData)
    {
        commander.ForceStatus = CharacterForceStatus.Prisoner;

        // 业务：主将被押至胜方当前所在据点的牢城
        var stronghold = gameData.Strongholds.Values.FirstOrDefault(s =>
            s.ForceId == victor.ForceId && s.Location.IsSameTile(victor.Location));

        if (stronghold is not null)
        {
            commander.StrongholdId = stronghold.Id;
            commander.Location = stronghold.Location;
            commander.LocationType = CharacterLocationType.Stronghold;
            commander.LocationStrongholdId = stronghold.Id;
        }
    }

    private static string FormatCommanderMessage(Unit destroyed, string? fate)
    {
        if (fate is null)
            return string.Empty;

        var name = destroyed.LeaderId > 0 ? $"将领" : destroyed.Name;
        return fate switch
        {
            "Captured" => $"{name} 被俘。",
            "Escaped" => $"{name} 率残部逃脱。",
            "Slain" => $"{name} 战死。",
            _ => string.Empty
        };
    }
}
