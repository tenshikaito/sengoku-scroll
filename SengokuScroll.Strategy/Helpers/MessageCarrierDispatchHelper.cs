using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>文书载体指令下达结果。</summary>
public enum MessageCarrierDispatchOutcome
{
    /// <summary>同格，已即时生效。</summary>
    AppliedImmediately,

    /// <summary>异格，已生成在途载体。</summary>
    CarrierDispatched
}

/// <summary>
/// 文书载体派遣：同格即时投递，异格生成载体实体沿路径传递。
/// </summary>
public class MessageCarrierDispatchHelper(
    IGameContext context,
    IPathfindingService pathfindingService)
{
    /// <summary>
    /// 向目标单位下达方针变更；同格免载体，异格自动生成在途载体。
    /// </summary>
    public MessageCarrierDispatchOutcome IssuePolicyChange(
        Point3 issuerLocation,
        int sourceStrongholdId,
        Unit targetUnit,
        UnitDirective directive)
    {
        if (!MessageCarrierRules.RequiresInTransitDelivery(issuerLocation, targetUnit.Location))
        {
            MessageCarrierActions.ApplyPolicyChange(targetUnit, directive);
            return MessageCarrierDispatchOutcome.AppliedImmediately;
        }

        var start = issuerLocation;
        var path = pathfindingService.CalculatePath(
            new MapPathAgent(start, targetUnit.ForceId),
            targetUnit.Location);

        if (path is null || path.Count <= 1)
        {
            MessageCarrierActions.ApplyPolicyChange(targetUnit, directive);
            return MessageCarrierDispatchOutcome.AppliedImmediately;
        }

        var gameData = context.GameWorldContext.GameWorld.GameData;
        var carrierId = NextEntityId(gameData.MessageCarriers.Keys);
        gameData.Strongholds.TryGetValue(sourceStrongholdId, out var origin);

        var carrier = new MessageCarrier
        {
            Id = carrierId,
            Name = BuildPolicyCarrierName(origin?.Name, targetUnit.Name),
            ForceId = targetUnit.ForceId,
            Location = start,
            SourceStrongholdId = sourceStrongholdId,
            CourierCount = LogisticsConstants.DefaultMessengerCourierCount,
            EscortSoldierCount = LogisticsConstants.DefaultMessengerEscortCount,
            CarrierKind = MessageCarrierKind.Character,
            Status = MessageCarrierStatus.Moving,
            RoutePoints = RouteCalculator.ToDailyRouteQueue(path),
            Payload = new MessagePayload
            {
                Type = MessagePayloadType.PolicyChange,
                TargetUnitId = targetUnit.Id,
                PendingDirective = directive
            }
        };

        gameData.MessageCarriers[carrierId] = carrier;
        return MessageCarrierDispatchOutcome.CarrierDispatched;
    }

    /// <summary>
    /// 向直辖据点下达税率变更；自居城派出载体，同格即时生效。
    /// </summary>
    public MessageCarrierDispatchOutcome IssueTaxRateChange(
        Point3 issuerLocation,
        int sourceStrongholdId,
        Stronghold targetStronghold,
        PendingStrongholdTaxChange taxChange)
    {
        if (!StrongholdDomesticRules.RequiresInTransitDeliveryForTaxChange(issuerLocation, targetStronghold))
        {
            MessageCarrierActions.ApplyTaxRateChange(targetStronghold, taxChange, out _);
            return MessageCarrierDispatchOutcome.AppliedImmediately;
        }

        var path = pathfindingService.CalculatePath(
            new MapPathAgent(issuerLocation, targetStronghold.ForceId),
            targetStronghold.Location);

        if (path is null || path.Count <= 1)
        {
            MessageCarrierActions.ApplyTaxRateChange(targetStronghold, taxChange, out _);
            return MessageCarrierDispatchOutcome.AppliedImmediately;
        }

        var gameData = context.GameWorldContext.GameWorld.GameData;
        var carrierId = NextEntityId(gameData.MessageCarriers.Keys);
        gameData.Strongholds.TryGetValue(sourceStrongholdId, out var origin);

        var carrier = new MessageCarrier
        {
            Id = carrierId,
            Name = BuildTaxCarrierName(origin?.Name, targetStronghold.Name),
            ForceId = targetStronghold.ForceId,
            Location = issuerLocation,
            SourceStrongholdId = sourceStrongholdId,
            CourierCount = LogisticsConstants.DefaultMessengerCourierCount,
            EscortSoldierCount = LogisticsConstants.DefaultMessengerEscortCount,
            CarrierKind = MessageCarrierKind.Character,
            Status = MessageCarrierStatus.Moving,
            RoutePoints = RouteCalculator.ToDailyRouteQueue(path),
            Payload = new MessagePayload
            {
                Type = MessagePayloadType.TaxRateChange,
                TargetStrongholdId = targetStronghold.Id,
                PendingTaxChange = taxChange
            }
        };

        gameData.MessageCarriers[carrierId] = carrier;
        return MessageCarrierDispatchOutcome.CarrierDispatched;
    }

    /// <summary>从己方部队所在格向当主所在格派出战报载体（异格时生成实体）。</summary>
    /// <returns>新建载体 Id；同格或路径不可达时返回 null。</returns>
    public int? DispatchBattleReport(
        Point3 origin,
        int forceId,
        int sourceStrongholdId,
        Point3 lordLocation)
    {
        var path = pathfindingService.CalculatePath(
            new MapPathAgent(origin, forceId),
            lordLocation);

        if (path is null)
            return null;

        var gameData = context.GameWorldContext.GameWorld.GameData;
        var carrierId = NextEntityId(gameData.MessageCarriers.Keys);
        gameData.Strongholds.TryGetValue(sourceStrongholdId, out var stronghold);

        var route = path.Count <= 1
            ? new Queue<Point3>()
            : RouteCalculator.ToDailyRouteQueue(path);

        var carrier = new MessageCarrier
        {
            Id = carrierId,
            Name = BuildBattleReportCarrierName(stronghold?.Name),
            ForceId = forceId,
            Location = origin,
            SourceStrongholdId = sourceStrongholdId,
            CourierCount = LogisticsConstants.DefaultMessengerCourierCount,
            EscortSoldierCount = LogisticsConstants.DefaultMessengerEscortCount,
            CarrierKind = MessageCarrierKind.UnitEscort,
            Status = MessageCarrierStatus.Moving,
            RoutePoints = route,
            Payload = new MessagePayload { Type = MessagePayloadType.BattleReport }
        };

        gameData.MessageCarriers[carrierId] = carrier;
        return carrierId;
    }

    /// <summary>从战场/据点向当主所在格派出战略情报载体。</summary>
    public int? DispatchStrategicReport(
        Point3 origin,
        int forceId,
        int sourceStrongholdId,
        Point3 lordLocation)
    {
        var path = pathfindingService.CalculatePath(
            new MapPathAgent(origin, forceId),
            lordLocation);

        if (path is null)
            return null;

        var gameData = context.GameWorldContext.GameWorld.GameData;
        var carrierId = NextEntityId(gameData.MessageCarriers.Keys);
        gameData.Strongholds.TryGetValue(sourceStrongholdId, out var stronghold);

        var route = path.Count <= 1
            ? new Queue<Point3>()
            : RouteCalculator.ToDailyRouteQueue(path);

        var carrier = new MessageCarrier
        {
            Id = carrierId,
            Name = BuildStrategicReportCarrierName(stronghold?.Name),
            ForceId = forceId,
            Location = origin,
            SourceStrongholdId = sourceStrongholdId,
            CourierCount = LogisticsConstants.DefaultMessengerCourierCount,
            EscortSoldierCount = LogisticsConstants.DefaultMessengerEscortCount,
            CarrierKind = MessageCarrierKind.UnitEscort,
            Status = MessageCarrierStatus.Moving,
            RoutePoints = route,
            Payload = new MessagePayload { Type = MessagePayloadType.StrategicReport }
        };

        gameData.MessageCarriers[carrierId] = carrier;
        return carrierId;
    }

    private static string BuildPolicyCarrierName(string? originName, string targetUnitName)
    {
        var origin = string.IsNullOrWhiteSpace(originName) ? "据点" : originName.Trim();
        return $"{origin}文书→{targetUnitName}";
    }

    private static string BuildTaxCarrierName(string? originName, string targetStrongholdName)
    {
        var origin = string.IsNullOrWhiteSpace(originName) ? "居城" : originName.Trim();
        return $"{origin}税令→{targetStrongholdName.Trim()}";
    }

    private static string BuildStrategicReportCarrierName(string? originName)
    {
        var origin = string.IsNullOrWhiteSpace(originName) ? "前线" : originName.Trim();
        return $"{origin}情报";
    }

    private static string BuildBattleReportCarrierName(string? originName)
    {
        var origin = string.IsNullOrWhiteSpace(originName) ? "据点" : originName.Trim();
        return $"{origin}战报";
    }

    private static int NextEntityId(IEnumerable<int> existingIds)
    {
        var max = existingIds.DefaultIfEmpty(0).Max();
        return max + 1;
    }
}
