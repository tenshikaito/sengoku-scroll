using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;

namespace SengokuScroll.Strategy.Calculators;

/// <summary>行政效率：距当主居城最短可移动路径越远，效率越低（引导设立内藩）。</summary>
public static class AdministrationCalculator
{
    /// <summary>距势力当主居城的最短可移动路径（格）；无居城、居城本身或内藩领地为 0。</summary>
    public static int CalculateCapitalPathDistanceTiles(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta,
        GameWorld? world = null)
    {
        if (stronghold.LordId > 0)
            return 0;

        var capitalId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            stronghold.ForceId,
            gameData,
            meta);
        if (capitalId <= 0 || capitalId == stronghold.Id)
            return 0;

        if (!gameData.Strongholds.TryGetValue(capitalId, out var capital))
            return 0;

        if (world is null)
        {
            return Math.Abs(stronghold.Location.X - capital.Location.X)
                   + Math.Abs(stronghold.Location.Y - capital.Location.Y);
        }

        var start = (Point2)capital.Location;
        var target = (Point2)stronghold.Location;
        if (start == target)
            return 0;

        return CalculateShortestPassablePathDistance(world, start, target);
    }

    /// <summary>因距居城过远产生的行政损耗（0–100），并入征收效率计算。</summary>
    public static byte CalculateDistanceAdministrativeLoss(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta,
        GameWorld? world = null)
    {
        if (stronghold.LordId > 0)
            return 0;

        var distance = CalculateCapitalPathDistanceTiles(stronghold, gameData, meta, world);
        if (distance <= 0)
            return 0;

        var lossPerTile = ResolveLossPerTile(meta);
        return (byte)Math.Min(100, distance * lossPerTile);
    }

    /// <summary>有效腐败 = 本地腐败 + 距离损耗（上限 100）。</summary>
    public static byte CalculateEffectiveCorruption(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta,
        GameWorld? world = null)
    {
        var distanceLoss = CalculateDistanceAdministrativeLoss(stronghold, gameData, meta, world);
        return (byte)Math.Min(100, stronghold.Corruption + distanceLoss);
    }

    /// <summary>行政效率 0–100（100 − 路径格数 × 每格损耗%；内藩/居城为 100）。</summary>
    public static byte CalculateAdministrativeEfficiencyPercent(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta,
        GameWorld? world = null)
    {
        if (stronghold.LordId > 0)
            return 100;

        var distance = CalculateCapitalPathDistanceTiles(stronghold, gameData, meta, world);
        if (distance <= 0)
            return 100;

        var lossPerTile = ResolveLossPerTile(meta);
        return (byte)Math.Clamp(100 - distance * lossPerTile, 0, 100);
    }

    /// <summary>兼容旧调用：曼哈顿距离（仅测试/无地图上下文）。</summary>
    public static int CalculateCapitalManhattanDistance(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
        => CalculateCapitalPathDistanceTiles(stronghold, gameData, meta, world: null);

    private static int ResolveLossPerTile(StrategyScenarioMeta meta)
        => Math.Clamp(meta.GameOptions.AdministrativeEfficiencyLossPerTile, 0, 100);

    private static int CalculateShortestPassablePathDistance(GameWorld world, Point2 start, Point2 target)
    {
        var tileMap = world.GameMapMasterData.TileMap;
        var queue = new Queue<(Point2 Point, int Distance)>();
        var visited = new HashSet<Point2> { start };
        queue.Enqueue((start, 0));

        while (queue.Count > 0)
        {
            var (current, distance) = queue.Dequeue();
            if (current == target)
                return distance;

            foreach (var next in Neighbors(current))
            {
                if (visited.Contains(next))
                    continue;

                if (tileMap.IsOutOfBounds(next) || !IsPassableTile(world, next))
                    continue;

                visited.Add(next);
                queue.Enqueue((next, distance + 1));
            }
        }

        return 100;
    }

    private static IEnumerable<Point2> Neighbors(Point2 point)
    {
        yield return new Point2(point.X + 1, point.Y);
        yield return new Point2(point.X - 1, point.Y);
        yield return new Point2(point.X, point.Y + 1);
        yield return new Point2(point.X, point.Y - 1);
    }

    private static bool IsPassableTile(GameWorld world, Point2 location)
    {
        var tileMap = world.GameMapMasterData.TileMap;
        if (tileMap.IsOutOfBounds(location))
            return false;

        var terrainId = tileMap.GetTerrain(location);
        if (!world.GameMapMasterData.Terrains.TryGetValue(terrainId, out var terrain))
            return false;

        return terrain.MovementCost > 0;
    }
}
