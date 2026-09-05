using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Policies.MessageDelivery;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式文书载体系统接口。</summary>
public interface IStrategyMessageCarrierSystem : IGameSystem
{
}

/// <summary>
/// 文书载体系统：每日推进载体移动，抵达后投递方针/战报或向运输 Unit 施加假情报。
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
        var toRemove = new List<int>();

        foreach (var carrier in carriers.Values.OrderBy(carrier => carrier.Id).ToList())
        {
            if (carrier.Status != MessageCarrierStatus.Moving)
                continue;

            if (carrier.RoutePoints.Count == 0)
            {
                Deliver(carrier, gameData);
                toRemove.Add(carrier.Id);
                continue;
            }

            carrier.Location = carrier.RoutePoints.Dequeue();

            if (carrier.RoutePoints.Count == 0)
            {
                Deliver(carrier, gameData);
                toRemove.Add(carrier.Id);
            }
        }

        foreach (var id in toRemove)
            carriers.Remove(id);
        pendingBattleReports.PruneMissingCarriers(gameData);
        pendingEvents.PruneMissingCarriers(gameData);
    }

    private void Deliver(MessageCarrier carrier, Domain.GameData gameData)
    {
        carrier.Status = MessageCarrierStatus.Arrived;

        MessagePayloadDeliveryRegistry.TryDeliver(new MessagePayloadDeliveryContext
        {
            Carrier = carrier,
            GameData = gameData,
            ScenarioMeta = scenarioMeta,
            DayOutcomeBuffer = dayOutcomeBuffer,
            PendingBattleReports = pendingBattleReports,
            PendingEvents = pendingEvents,
            BattleReportDeliveryHelper = battleReportDeliveryHelper
        });
    }
}
