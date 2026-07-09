using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Data.Models;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式 AI 系统接口。</summary>
public interface IStrategyAISystem : IAISystem
{
}

/// <summary>
/// 简单 AI（M3-d）：非玩家势力闲置单位周期性向最近玩家据点寻路移动（边境巡逻/弱扩张）。
/// </summary>
public class StrategyAISystem(
    IGameContext context,
    StrategyScenarioMeta scenarioMeta,
    IPathfindingService pathfinding) : IStrategyAISystem
{
    /// <summary>在单位与信使之后执行。</summary>
    public int Order { get; } = 45;

    /// <inheritdoc />
    public void Update()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;

        // 每 3 日评估一次，避免每日频繁改道。
        if (gameData.GameDate.Day % 3 != 0)
            return;

        var playerStrongholds = gameData.Strongholds.Values
            .Where(s => s.ForceId == scenarioMeta.PlayerForceId)
            .ToList();

        if (playerStrongholds.Count == 0)
            return;

        foreach (var unit in context.GameWorldContext.EachUnit())
        {
            if (unit.ForceId == scenarioMeta.PlayerForceId)
                continue;

            if (!unit.IsMilitary || unit.Soldier <= 0)
                continue;

            if (unit.Status == UnitStatus.Moving)
                continue;

            if (unit.ActionTarget.RoutePoints.Count > 0)
                continue;

            TryAdvanceTowardPlayerTerritory(unit, playerStrongholds);
        }
    }

    private void TryAdvanceTowardPlayerTerritory(Unit unit, IReadOnlyList<Stronghold> playerStrongholds)
    {
        var unitPoint = (Point2)unit.Location;
        var target = playerStrongholds
            .OrderBy(s => Manhattan(unitPoint, (Point2)s.Location))
            .First();

        var path = pathfinding.CalculatePath(unit, (Point2)target.Location);
        if (path is null || path.Count < 2)
            return;

        unit.Status = UnitStatus.Moving;
        unit.ActionTarget.RoutePoints.Clear();

        foreach (var node in path.Skip(1))
            unit.ActionTarget.RoutePoints.Enqueue(node.Location);
    }

    private static int Manhattan(Point2 a, Point2 b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}
