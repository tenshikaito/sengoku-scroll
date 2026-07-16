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

/// <summary>信使指令下达结果。</summary>
public enum MessengerDispatchOutcome
{
    /// <summary>同格，已即时生效。</summary>
    AppliedImmediately,

    /// <summary>异格，已生成在途信使。</summary>
    MessengerDispatched
}

/// <summary>
/// 信使派遣：同格即时投递方针，异格生成信使实体沿路径传递。
/// </summary>
public class MessengerDispatchHelper(
    IGameContext context,
    IPathfindingService pathfindingService)
{
    /// <summary>
    /// 向目标单位下达方针变更；同格免信使，异格自动生成信使。
    /// </summary>
    /// <param name="issuerLocation">指令下达方所在格（君主/据点）。</param>
    /// <param name="sourceStrongholdId">出发据点 Id（信使路径起点）。</param>
    /// <param name="targetUnit">接收方针的单位。</param>
    /// <param name="directive">新方针。</param>
    public MessengerDispatchOutcome IssuePolicyChange(
        Point3 issuerLocation,
        int sourceStrongholdId,
        Unit targetUnit,
        UnitDirective directive)
    {
        // 业务：同格或寻路失败则即时生效，不生成信使实体
        if (!MessengerRules.RequiresMessenger(issuerLocation, targetUnit.Location))
        {
            MessengerActions.ApplyPolicyChange(targetUnit, directive);
            return MessengerDispatchOutcome.AppliedImmediately;
        }

        var start = issuerLocation;
        var path = pathfindingService.CalculatePath(
            new MapPathAgent(start, targetUnit.ForceId),
            targetUnit.Location);

        if (path is null || path.Count <= 1)
        {
            MessengerActions.ApplyPolicyChange(targetUnit, directive);
            return MessengerDispatchOutcome.AppliedImmediately;
        }

        var gameData = context.GameWorldContext.GameWorld.GameData;
        var messengerId = NextEntityId(gameData.Messengers.Keys);
        gameData.Strongholds.TryGetValue(sourceStrongholdId, out var origin);

        var messenger = new Messenger
        {
            Id = messengerId,
            Name = BuildPolicyMessengerName(origin?.Name, targetUnit.Name),
            ForceId = targetUnit.ForceId,
            Location = start,
            SourceStrongholdId = sourceStrongholdId,
            TargetUnitId = targetUnit.Id,
            CourierCount = LogisticsConstants.DefaultMessengerCourierCount,
            EscortSoldierCount = LogisticsConstants.DefaultMessengerEscortCount,
            PayloadType = MessengerPayloadType.PolicyChange,
            Status = MessengerStatus.Moving,
            PendingDirective = directive,
            RoutePoints = RouteCalculator.ToDailyRouteQueue(path)
        };

        gameData.Messengers[messengerId] = messenger;
        return MessengerDispatchOutcome.MessengerDispatched;
    }

    /// <summary>从己方部队所在格向当主所在格派出战报信使（异格时生成实体）。</summary>
    /// <returns>新建信使 Id；同格或路径不可达时返回 null。</returns>
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
        var messengerId = NextEntityId(gameData.Messengers.Keys);
        gameData.Strongholds.TryGetValue(sourceStrongholdId, out var stronghold);

        var route = path.Count <= 1
            ? new Queue<Point3>()
            : RouteCalculator.ToDailyRouteQueue(path);

        var messenger = new Messenger
        {
            Id = messengerId,
            Name = BuildBattleReportMessengerName(stronghold?.Name),
            ForceId = forceId,
            Location = origin,
            SourceStrongholdId = sourceStrongholdId,
            TargetUnitId = 0,
            CourierCount = LogisticsConstants.DefaultMessengerCourierCount,
            EscortSoldierCount = LogisticsConstants.DefaultMessengerEscortCount,
            PayloadType = MessengerPayloadType.BattleReport,
            Status = MessengerStatus.Moving,
            RoutePoints = route
        };

        gameData.Messengers[messengerId] = messenger;
        return messengerId;
    }

    /// <summary>从战场/据点向当主所在格派出战略情报信使。</summary>
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
        var messengerId = NextEntityId(gameData.Messengers.Keys);
        gameData.Strongholds.TryGetValue(sourceStrongholdId, out var stronghold);

        var route = path.Count <= 1
            ? new Queue<Point3>()
            : RouteCalculator.ToDailyRouteQueue(path);

        var messenger = new Messenger
        {
            Id = messengerId,
            Name = BuildStrategicReportMessengerName(stronghold?.Name),
            ForceId = forceId,
            Location = origin,
            SourceStrongholdId = sourceStrongholdId,
            TargetUnitId = 0,
            CourierCount = LogisticsConstants.DefaultMessengerCourierCount,
            EscortSoldierCount = LogisticsConstants.DefaultMessengerEscortCount,
            PayloadType = MessengerPayloadType.StrategicReport,
            Status = MessengerStatus.Moving,
            RoutePoints = route
        };

        gameData.Messengers[messengerId] = messenger;
        return messengerId;
    }

    private static string BuildPolicyMessengerName(string? originName, string targetUnitName)
    {
        var origin = string.IsNullOrWhiteSpace(originName) ? "据点" : originName.Trim();
        return $"{origin}信使→{targetUnitName}";
    }

    private static string BuildStrategicReportMessengerName(string? originName)
    {
        var origin = string.IsNullOrWhiteSpace(originName) ? "前线" : originName.Trim();
        return $"{origin}情报信使";
    }

    private static string BuildBattleReportMessengerName(string? originName)
    {
        var origin = string.IsNullOrWhiteSpace(originName) ? "据点" : originName.Trim();
        return $"{origin}战报信使";
    }

    private static int NextEntityId(IEnumerable<int> existingIds)
    {
        var max = existingIds.DefaultIfEmpty(0).Max();
        return max + 1;
    }
}
