using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式文书载体系统接口。</summary>
public interface IStrategyMessageCarrierSystem : IGameSystem
{
}

/// <summary>
/// 文书载体系统：每日推进载体移动，抵达后投递方针/战报或向运输队施加假情报。
/// </summary>
public class StrategyMessageCarrierSystem(
    IGameContext context,
    StrategyScenarioMeta scenarioMeta,
    StrategyDayOutcomeBuffer dayOutcomeBuffer,
    StrategyPendingBattleReportStore pendingBattleReports,
    StrategyPendingEventStore pendingEvents,
    BattleReportDeliveryHelper battleReportDeliveryHelper) : IStrategyMessageCarrierSystem
{
    /// <summary>在单位系统之后、角色系统之前执行。</summary>
    public int Order { get; } = 25;

    /// <inheritdoc />
    public void Update()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;
        var carriers = gameData.MessageCarriers;
        var convoys = gameData.SupplyConvoys;
        var toRemove = new List<int>();

        foreach (var carrier in carriers.Values.ToList())
        {
            if (carrier.Status != MessageCarrierStatus.Moving)
                continue;

            if (carrier.RoutePoints.Count == 0)
            {
                Deliver(carrier, convoys, gameData);
                toRemove.Add(carrier.Id);
                continue;
            }

            carrier.Location = carrier.RoutePoints.Dequeue();

            if (carrier.RoutePoints.Count == 0)
            {
                Deliver(carrier, convoys, gameData);
                toRemove.Add(carrier.Id);
            }
        }

        foreach (var id in toRemove)
            carriers.Remove(id);
    }

    private void Deliver(
        MessageCarrier carrier,
        Dictionary<int, SupplyConvoy> convoys,
        Domain.GameData gameData)
    {
        carrier.Status = MessageCarrierStatus.Arrived;

        if (carrier.Payload.Type == MessagePayloadType.PolicyChange
            && gameData.Units.TryGetValue(carrier.Payload.TargetUnitId, out var unit))
        {
            MessageCarrierActions.DeliverPendingPolicy(carrier, unit);
            NotifyPolicyDelivered(carrier, unit, gameData);
            return;
        }

        if (carrier.Payload.Type == MessagePayloadType.TaxRateChange
            && carrier.Payload.TargetStrongholdId > 0
            && gameData.Strongholds.TryGetValue(carrier.Payload.TargetStrongholdId, out var stronghold))
        {
            MessageCarrierActions.DeliverPendingTaxChange(carrier, stronghold);
            NotifyTaxRateDelivered(carrier, stronghold);
            return;
        }

        if (carrier.Payload.Type == MessagePayloadType.BattleReport)
        {
            NotifyBattleReportArrived(carrier, gameData);
            return;
        }

        if (carrier.Payload.Type == MessagePayloadType.StrategicReport)
        {
            NotifyStrategicReportArrived(carrier, gameData);
            return;
        }

        if (carrier.Payload.Type != MessagePayloadType.FalseIntelligence
            || carrier.Payload.TargetConvoyId <= 0
            || !convoys.TryGetValue(carrier.Payload.TargetConvoyId, out var convoy))
        {
            return;
        }

        MessageCarrierActions.ApplyFalseIntelligence(convoy, carrier);
    }

    private void NotifyPolicyDelivered(MessageCarrier carrier, Unit unit, Domain.GameData gameData)
    {
        if (carrier.ForceId != scenarioMeta.PlayerForceId)
            return;

        var directive = carrier.Payload.PendingDirective?.ToString() ?? "未知";
        dayOutcomeBuffer.AddEvent(new StrategyEventDto
        {
            Category = "PolicyDelivered",
            Message = $"📨 方针已传达至 {unit.Name}：{DirectiveLabel(directive)}"
        });
    }

    private void NotifyTaxRateDelivered(MessageCarrier carrier, Stronghold stronghold)
    {
        if (carrier.ForceId != scenarioMeta.PlayerForceId)
            return;

        dayOutcomeBuffer.AddEvent(new StrategyEventDto
        {
            Category = "TaxRateDelivered",
            Message = $"📨 税令已传达至 {stronghold.Name}，新税率已生效"
        });
    }

    private void NotifyBattleReportArrived(MessageCarrier carrier, Domain.GameData gameData)
    {
        if (carrier.ForceId != scenarioMeta.PlayerForceId)
            return;

        var battleResult = pendingBattleReports.Take(carrier.Id);
        if (battleResult is null)
        {
            dayOutcomeBuffer.AddEvent(new StrategyEventDto
            {
                Category = "BattleReportArrived",
                Message =
                    $"📨 战报已送达（{carrier.Location.X}, {carrier.Location.Y}）"
            });
            return;
        }

        battleReportDeliveryHelper.NotifyPlayerBattleReportArrivedFromMessenger(
            battleResult,
            carrier.Location,
            gameData);
    }

    private void NotifyStrategicReportArrived(MessageCarrier carrier, Domain.GameData gameData)
    {
        if (carrier.ForceId != scenarioMeta.PlayerForceId)
            return;

        var reportEvent = pendingEvents.Take(carrier.Id);
        if (reportEvent is null)
        {
            dayOutcomeBuffer.AddEvent(new StrategyEventDto
            {
                Category = "StrategicReportArrived",
                Message = $"📨 情报已送达（{carrier.Location.X}, {carrier.Location.Y}）"
            });
            return;
        }

        battleReportDeliveryHelper.NotifyPlayerStrategicReportArrivedFromMessenger(
            reportEvent,
            carrier.Location,
            gameData);
    }

    private static string DirectiveLabel(string directive) => directive switch
    {
        "Move" => "移动",
        "Occupy" => "占领",
        "Raid" => "劫掠",
        "Support" => "支援",
        "Retreat" => "撤退",
        _ => directive
    };
}
