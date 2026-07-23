using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Policies.MessageDelivery;

public sealed class MessagePayloadDeliveryContext
{
    public required MessageCarrier Carrier { get; init; }
    public required Dictionary<int, SupplyConvoy> Convoys { get; init; }
    public required GameData GameData { get; init; }
    public required StrategyScenarioMeta ScenarioMeta { get; init; }
    public required StrategyDayOutcomeBuffer DayOutcomeBuffer { get; init; }
    public required StrategyPendingBattleReportStore PendingBattleReports { get; init; }
    public required StrategyPendingEventStore PendingEvents { get; init; }
    public required BattleReportDeliveryHelper BattleReportDeliveryHelper { get; init; }
}

public interface IMessagePayloadDeliveryHandler
{
    MessagePayloadType PayloadType { get; }

    bool TryDeliver(MessagePayloadDeliveryContext ctx);
}

internal sealed class PolicyChangePayloadHandler : IMessagePayloadDeliveryHandler
{
    public static readonly PolicyChangePayloadHandler Instance = new();
    public MessagePayloadType PayloadType => MessagePayloadType.PolicyChange;

    public bool TryDeliver(MessagePayloadDeliveryContext ctx)
    {
        var carrier = ctx.Carrier;
        if (!ctx.GameData.Units.TryGetValue(carrier.Payload.TargetUnitId, out var unit))
            return false;

        MessageCarrierActions.DeliverPendingPolicy(carrier, unit);
        MessageCarrierNotificationHelper.NotifyPolicyDelivered(ctx, unit);
        return true;
    }
}

internal sealed class TaxRateChangePayloadHandler : IMessagePayloadDeliveryHandler
{
    public static readonly TaxRateChangePayloadHandler Instance = new();
    public MessagePayloadType PayloadType => MessagePayloadType.TaxRateChange;

    public bool TryDeliver(MessagePayloadDeliveryContext ctx)
    {
        var carrier = ctx.Carrier;
        if (carrier.Payload.TargetStrongholdId <= 0
            || !ctx.GameData.Strongholds.TryGetValue(carrier.Payload.TargetStrongholdId, out var stronghold))
            return false;

        MessageCarrierActions.DeliverPendingTaxChange(carrier, stronghold);
        MessageCarrierNotificationHelper.NotifyTaxRateDelivered(ctx, stronghold);
        return true;
    }
}

internal sealed class GovernancePriorityChangePayloadHandler : IMessagePayloadDeliveryHandler
{
    public static readonly GovernancePriorityChangePayloadHandler Instance = new();
    public MessagePayloadType PayloadType => MessagePayloadType.GovernancePriorityChange;

    public bool TryDeliver(MessagePayloadDeliveryContext ctx)
    {
        var carrier = ctx.Carrier;
        if (carrier.Payload.TargetStrongholdId <= 0
            || !ctx.GameData.Strongholds.TryGetValue(carrier.Payload.TargetStrongholdId, out var stronghold))
            return false;

        if (!MessageCarrierActions.DeliverPendingGovernanceChange(
                carrier,
                stronghold,
                ctx.GameData,
                ctx.ScenarioMeta))
        {
            return false;
        }

        MessageCarrierNotificationHelper.NotifyGovernancePriorityDelivered(ctx, stronghold);
        return true;
    }
}

internal sealed class CharacterRecallPayloadHandler : IMessagePayloadDeliveryHandler
{
    public static readonly CharacterRecallPayloadHandler Instance = new();
    public MessagePayloadType PayloadType => MessagePayloadType.CharacterRecall;

    public bool TryDeliver(MessagePayloadDeliveryContext ctx)
    {
        var carrier = ctx.Carrier;
        var targetCharacterId = carrier.Payload.TargetCharacterId;
        ctx.GameData.Characters.TryGetValue(targetCharacterId, out var targetCharacter);

        if (!MessageCarrierActions.DeliverCharacterRecall(
                carrier,
                ctx.GameData,
                ctx.ScenarioMeta))
        {
            return false;
        }

        MessageCarrierNotificationHelper.NotifyCharacterRecallDelivered(ctx, targetCharacter?.Name);
        return true;
    }
}

internal sealed class BattleReportPayloadHandler : IMessagePayloadDeliveryHandler
{
    public static readonly BattleReportPayloadHandler Instance = new();
    public MessagePayloadType PayloadType => MessagePayloadType.BattleReport;

    public bool TryDeliver(MessagePayloadDeliveryContext ctx)
    {
        MessageCarrierNotificationHelper.NotifyBattleReportArrived(ctx);
        return true;
    }
}

internal sealed class StrategicReportPayloadHandler : IMessagePayloadDeliveryHandler
{
    public static readonly StrategicReportPayloadHandler Instance = new();
    public MessagePayloadType PayloadType => MessagePayloadType.StrategicReport;

    public bool TryDeliver(MessagePayloadDeliveryContext ctx)
    {
        MessageCarrierNotificationHelper.NotifyStrategicReportArrived(ctx);
        return true;
    }
}

internal sealed class FalseIntelligencePayloadHandler : IMessagePayloadDeliveryHandler
{
    public static readonly FalseIntelligencePayloadHandler Instance = new();
    public MessagePayloadType PayloadType => MessagePayloadType.FalseIntelligence;

    public bool TryDeliver(MessagePayloadDeliveryContext ctx)
    {
        var carrier = ctx.Carrier;
        if (carrier.Payload.TargetConvoyId <= 0
            || !ctx.Convoys.TryGetValue(carrier.Payload.TargetConvoyId, out var convoy))
            return false;

        MessageCarrierActions.ApplyFalseIntelligence(convoy, carrier);
        return true;
    }
}

public static class MessagePayloadDeliveryRegistry
{
    private static readonly IMessagePayloadDeliveryHandler[] Ordered =
    [
        PolicyChangePayloadHandler.Instance,
        TaxRateChangePayloadHandler.Instance,
        GovernancePriorityChangePayloadHandler.Instance,
        CharacterRecallPayloadHandler.Instance,
        BattleReportPayloadHandler.Instance,
        StrategicReportPayloadHandler.Instance,
        FalseIntelligencePayloadHandler.Instance
    ];

    private static readonly Dictionary<MessagePayloadType, IMessagePayloadDeliveryHandler> ByType =
        Ordered.ToDictionary(h => h.PayloadType);

    public static bool TryDeliver(MessagePayloadDeliveryContext ctx)
    {
        if (!ByType.TryGetValue(ctx.Carrier.Payload.Type, out var handler))
            return false;

        return handler.TryDeliver(ctx);
    }
}

internal static class MessageCarrierNotificationHelper
{
    public static void NotifyPolicyDelivered(MessagePayloadDeliveryContext ctx, Unit unit)
    {
        if (ctx.Carrier.ForceId != ctx.ScenarioMeta.PlayerForceId)
            return;

        var directive = ctx.Carrier.Payload.PendingDirective?.ToString() ?? "未知";
        ctx.DayOutcomeBuffer.AddEvent(new StrategyEventDto
        {
            Category = "PolicyDelivered",
            Message = $"📨 方针已传达至 {unit.Name}：{UnitDirectiveLabelRegistry.Label(directive)}"
        });
    }

    public static void NotifyTaxRateDelivered(MessagePayloadDeliveryContext ctx, Stronghold stronghold)
    {
        if (ctx.Carrier.ForceId != ctx.ScenarioMeta.PlayerForceId)
            return;

        ctx.DayOutcomeBuffer.AddEvent(new StrategyEventDto
        {
            Category = "TaxRateDelivered",
            Message = $"📨 税令已传达至 {stronghold.Name}，新税率已生效"
        });
    }

    public static void NotifyGovernancePriorityDelivered(
        MessagePayloadDeliveryContext ctx,
        Stronghold stronghold)
    {
        if (ctx.Carrier.ForceId != ctx.ScenarioMeta.PlayerForceId)
            return;

        var label = StrongholdGovernancePriorityLabelRegistry.Label(stronghold.GovernancePriority);
        ctx.DayOutcomeBuffer.AddEvent(new StrategyEventDto
        {
            Category = "GovernancePriorityDelivered",
            Message = $"📨 方针已传达至 {stronghold.Name}：{label}"
        });
    }

    public static void NotifyCharacterRecallDelivered(MessagePayloadDeliveryContext ctx, string? characterName = null)
    {
        if (ctx.Carrier.ForceId != ctx.ScenarioMeta.PlayerForceId)
            return;

        var name = string.IsNullOrWhiteSpace(characterName) ? "将领" : characterName.Trim();
        ctx.DayOutcomeBuffer.AddEvent(new StrategyEventDto
        {
            Category = "CharacterRecallDelivered",
            Message = $"📨 召回令已传达，{name} 正尽快回城"
        });
    }

    public static void NotifyBattleReportArrived(MessagePayloadDeliveryContext ctx)
    {
        if (ctx.Carrier.ForceId != ctx.ScenarioMeta.PlayerForceId)
            return;

        var carrier = ctx.Carrier;
        var battleResult = ctx.PendingBattleReports.Take(carrier.Id);
        if (battleResult is null)
        {
            ctx.DayOutcomeBuffer.AddEvent(new StrategyEventDto
            {
                Category = "BattleReportArrived",
                Message = $"📨 战报已送达（{carrier.Location.X}, {carrier.Location.Y}）"
            });
            return;
        }

        ctx.BattleReportDeliveryHelper.NotifyPlayerBattleReportArrivedFromMessenger(
            battleResult,
            carrier.Location,
            ctx.GameData);
    }

    public static void NotifyStrategicReportArrived(MessagePayloadDeliveryContext ctx)
    {
        if (ctx.Carrier.ForceId != ctx.ScenarioMeta.PlayerForceId)
            return;

        var carrier = ctx.Carrier;
        var reportEvent = ctx.PendingEvents.Take(carrier.Id);
        if (reportEvent is null)
        {
            ctx.DayOutcomeBuffer.AddEvent(new StrategyEventDto
            {
                Category = "StrategicReportArrived",
                Message = $"📨 情报已送达（{carrier.Location.X}, {carrier.Location.Y}）"
            });
            return;
        }

        ctx.BattleReportDeliveryHelper.NotifyPlayerStrategicReportArrivedFromMessenger(
            reportEvent,
            carrier.Location,
            ctx.GameData);
    }
}

public static class UnitDirectiveLabelRegistry
{
    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Move"] = "移动",
        ["Occupy"] = "占领",
        ["Raid"] = "劫掠",
        ["Support"] = "支援",
        ["Retreat"] = "撤退"
    };

    public static string Label(string directive)
        => Labels.TryGetValue(directive, out var label) ? label : directive;
}

public static class StrongholdGovernancePriorityLabelRegistry
{
    public static string Label(StrongholdGovernancePriority priority)
        => priority switch
        {
            StrongholdGovernancePriority.Military => "军事优先",
            StrongholdGovernancePriority.Domestic => "内政优先",
            StrongholdGovernancePriority.Autonomous => "自由决策",
            _ => priority.ToString()
        };
}
