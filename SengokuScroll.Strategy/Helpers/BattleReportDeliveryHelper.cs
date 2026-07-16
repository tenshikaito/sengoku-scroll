using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>战报与战略情报：玩家消息须经信使抵达当主/居城后方可查看。</summary>
public sealed class BattleReportDeliveryHelper(
    MessengerDispatchHelper messengerDispatchHelper,
    StrategyPendingBattleReportStore pendingReports,
    StrategyPendingEventStore pendingEvents,
    StrategyDayOutcomeBuffer dayOutcomeBuffer,
    StrategyScenarioMeta scenarioMeta)
{
    private readonly HashSet<string> playerNotifiedBattleKeys = new(StringComparer.Ordinal);
    private readonly HashSet<string> playerNotifiedEventKeys = new(StringComparer.Ordinal);

    /// <summary>向相关势力派送决战战报（玩家须等信使抵达）。</summary>
    public void DeliverDecisiveBattleReport(
        int forceId,
        Point3 origin,
        GameData gameData,
        InstantBattleOutcome outcome,
        Unit attacker,
        Unit defender,
        StrategyBattleResultDto battleResult,
        IReadOnlyList<int>? attackerParticipantIds = null,
        IReadOnlyList<int>? defenderParticipantIds = null)
    {
        if (!BattleReportDispatchRules.ShouldDispatchBattleReport(
                forceId,
                outcome,
                attacker,
                defender,
                scenarioMeta.PlayerForceId,
                gameData,
                attackerParticipantIds,
                defenderParticipantIds))
            return;

        var destinations = BattleReportRoutingHelper.ResolveDestinations(
            forceId, scenarioMeta, gameData, attacker, defender);

        foreach (var destination in destinations)
            DeliverBattleReportToDestination(forceId, origin, gameData, battleResult, destination);
    }

    /// <summary>攻城令下达时向玩家势力派送战略情报（非战斗结果对话框）。</summary>
    public void DeliverSiegeOrderStartedReport(
        Unit attacker,
        Stronghold target,
        UnitSiegeMode mode,
        GameData gameData,
        Unit? defenderUnit = null)
    {
        var playerForceId = scenarioMeta.PlayerForceId;
        if (attacker.ForceId != playerForceId && target.ForceId != playerForceId)
            return;

        var modeLabel = mode == UnitSiegeMode.Assault ? "强攻" : "包围";
        var reportEvent = new StrategyEventDto
        {
            Category = "SiegeOrderStarted",
            Brief = mode == UnitSiegeMode.Assault
                ? $"⚔ {attacker.Name} 强攻 {target.Name}"
                : $"⭕ {attacker.Name} 包围 {target.Name}",
            Message =
                $"{attacker.Name} 对 {target.Name} 发动{modeLabel}。" +
                (defenderUnit is not null
                    ? $" 守军 {defenderUnit.Name}（{defenderUnit.Soldier} 人）据守城格。"
                    : $" 城内驻军约 {target.ForceActor.Soldier} 人。"),
            DetailCategory = mode == UnitSiegeMode.Assault ? "SiegeAssault" : "SiegeEncircle"
        };

        DeliverPlayerStrategicReport(
            playerForceId,
            attacker.Location,
            gameData,
            reportEvent);
    }

    /// <summary>向玩家势力派送战略情报（溃灭、占城等），须经信使抵达后方可展示。</summary>
    public void DeliverPlayerStrategicReport(
        int forceId,
        Point3 origin,
        GameData gameData,
        StrategyEventDto reportEvent)
    {
        if (forceId != scenarioMeta.PlayerForceId)
            return;

        var lordLocation = StrategyLordHelper.ResolveLocation(gameData, scenarioMeta);
        var strongholdId = StrategyLordHelper.ResolveSourceStrongholdId(gameData, scenarioMeta, lordLocation);
        var label = ResolveArrivalLabel(lordLocation, gameData);
        DeliverStrategicReportToDestination(
            forceId,
            origin,
            reportEvent,
            new BattleReportRoutingHelper.BattleReportDestination(lordLocation, strongholdId, label));
    }

    private void DeliverBattleReportToDestination(
        int forceId,
        Point3 origin,
        GameData gameData,
        StrategyBattleResultDto battleResult,
        BattleReportRoutingHelper.BattleReportDestination destination)
    {
        if (forceId != scenarioMeta.PlayerForceId)
            return;

        var messengerId = messengerDispatchHelper.DispatchBattleReport(
            origin,
            forceId,
            destination.SourceStrongholdId,
            destination.Location);

        if (messengerId is int id)
        {
            pendingReports.Attach(id, battleResult);
            return;
        }

        NotifyPlayerBattleReportArrived(
            battleResult,
            destination,
            sameTile: false,
            deliveryFailed: true);
    }

    private void DeliverStrategicReportToDestination(
        int forceId,
        Point3 origin,
        StrategyEventDto reportEvent,
        BattleReportRoutingHelper.BattleReportDestination destination)
    {
        if (forceId != scenarioMeta.PlayerForceId)
            return;

        var messengerId = messengerDispatchHelper.DispatchStrategicReport(
            origin,
            forceId,
            destination.SourceStrongholdId,
            destination.Location);

        if (messengerId is int id)
        {
            pendingEvents.Attach(id, reportEvent);
            return;
        }

        NotifyPlayerStrategicReportArrived(reportEvent, destination, deliveryFailed: true);
    }

    /// <summary>向玩家事件栏推送战报（仅信使抵达或投递失败时调用）。</summary>
    public void NotifyPlayerBattleReportArrived(
        StrategyBattleResultDto battleResult,
        BattleReportRoutingHelper.BattleReportDestination destination,
        bool sameTile,
        bool immediateDelivery = false,
        bool deliveryFailed = false)
    {
        var battleKey = BuildBattleKey(battleResult);
        if (!playerNotifiedBattleKeys.Add(battleKey))
            return;

        var brief = BuildBrief(battleResult);
        var message = deliveryFailed
            ? $"⚠ 前线战报无法送达 {destination.Label}（道路不通）：{brief}"
            : $"📨 战报信使抵达 {destination.Label}（{destination.Location.X}, {destination.Location.Y}）：{brief}";

        dayOutcomeBuffer.AddEvent(new StrategyEventDto
        {
            Category = "BattleReportArrived",
            Message = message,
            Brief = brief,
            BattleResult = battleResult
        });
    }

    /// <summary>战略情报信使抵达后推送玩家事件。</summary>
    public void NotifyPlayerStrategicReportArrived(
        StrategyEventDto reportEvent,
        BattleReportRoutingHelper.BattleReportDestination destination,
        bool deliveryFailed = false)
    {
        var eventKey = $"{reportEvent.Category}:{reportEvent.Brief}:{destination.Location.X}:{destination.Location.Y}";
        if (!playerNotifiedEventKeys.Add(eventKey))
            return;

        var brief = reportEvent.Brief?.Trim() ?? reportEvent.Message;
        var message = deliveryFailed
            ? $"⚠ 情报无法送达 {destination.Label}（道路不通）：{brief}"
            : $"📨 情报信使抵达 {destination.Label}（{destination.Location.X}, {destination.Location.Y}）：{brief}";

        dayOutcomeBuffer.AddEvent(new StrategyEventDto
        {
            Category = "StrategicReportArrived",
            Message = message,
            Brief = brief,
            DetailCategory = reportEvent.Category,
            DetailMessage = reportEvent.Message
        });
    }

    /// <summary>战报信使抵达后，解析抵达标签并推送玩家事件。</summary>
    public void NotifyPlayerBattleReportArrivedFromMessenger(
        StrategyBattleResultDto battleResult,
        Point3 arrivalLocation,
        GameData gameData)
    {
        var label = ResolveArrivalLabel(arrivalLocation, gameData);
        var strongholdId = StrategyLordHelper.ResolveSourceStrongholdId(gameData, scenarioMeta, arrivalLocation);
        NotifyPlayerBattleReportArrived(
            battleResult,
            new BattleReportRoutingHelper.BattleReportDestination(arrivalLocation, strongholdId, label),
            sameTile: false);
    }

    /// <summary>战略情报信使抵达当主所在格。</summary>
    public void NotifyPlayerStrategicReportArrivedFromMessenger(
        StrategyEventDto reportEvent,
        Point3 arrivalLocation,
        GameData gameData)
    {
        var label = ResolveArrivalLabel(arrivalLocation, gameData);
        var strongholdId = StrategyLordHelper.ResolveSourceStrongholdId(gameData, scenarioMeta, arrivalLocation);
        NotifyPlayerStrategicReportArrived(
            reportEvent,
            new BattleReportRoutingHelper.BattleReportDestination(arrivalLocation, strongholdId, label));
    }

    private string ResolveArrivalLabel(Point3 location, GameData gameData)
    {
        var atStronghold = gameData.Strongholds.Values.FirstOrDefault(s =>
            s.ForceId == scenarioMeta.PlayerForceId
            && s.Location.X == location.X
            && s.Location.Y == location.Y);

        if (atStronghold is not null)
        {
            var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
                scenarioMeta.PlayerForceId, gameData, scenarioMeta);
            if (atStronghold.Id == residenceId)
                return "居城";

            return atStronghold.Name;
        }

        var lordLocation = StrategyLordHelper.ResolveLocation(gameData, scenarioMeta);
        if (lordLocation.X == location.X && lordLocation.Y == location.Y)
            return scenarioMeta.LordName;

        return "当主";
    }

    private static string BuildBattleKey(StrategyBattleResultDto result)
        => $"{result.ResolutionSeed}:{result.AttackerUnitId}:{result.DefenderUnitId}";

    private static string BuildBrief(StrategyBattleResultDto result)
    {
        if (result.EngagementKind is "SiegeAssault" or "SiegeEncircle")
            return $"{result.AttackerName} → {result.DefenderName}（{result.EngagementKind switch
            {
                "SiegeAssault" => "强攻",
                "SiegeEncircle" => "包围",
                _ => "围城"
            }}）";

        var main = $"{result.AttackerName} vs {result.DefenderName}";
        var extras = result.AttackerReinforcementNames.Concat(result.DefenderReinforcementNames).ToList();
        if (extras.Count == 0)
            return main;

        return $"{main}（驰援：{string.Join("、", extras)}）";
    }
}
